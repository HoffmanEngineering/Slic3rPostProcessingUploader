using System.Globalization;
using System.Text.Json;

namespace PrintLog.SlicerSettings
{
    /// <summary>
    /// Parses a whole G-code file (or its head and tail) that a slicer plugin uploaded, together with what the plugin
    /// measured and read on the slicing machine, into the settings the uploader would have posted for the same file.
    /// </summary>
    public static class SlicerGcodeParser
    {
        /// <summary>At most this many <see cref="SlicerGcodeOptions.Models"/> entries are used; the rest are ignored.</summary>
        internal const int MaxModels = 1000;

        /// <summary>Longer <see cref="ModelBounds.Name"/>s are cut to this many characters.</summary>
        internal const int MaxModelNameLength = 256;

        /// <summary>
        /// Detects the slicer, renders the requested built-in note template, extracts the thumbnail, print time and
        /// filament usage, and returns the result as the <c>settings</c> object the uploader posts to
        /// <c>/api/Cura/settings</c>.
        /// </summary>
        /// <param name="gcode">
        /// The G-code as text. A file over 4,000,000 characters may be sent as its first and last 2,000,000 characters
        /// joined by a newline (the uploader reads large files the same way); everything the parser reads is in those
        /// regions. Binary G-code (<c>.bgcode</c>) is not decoded: send text.
        /// </param>
        /// <param name="options">The note template and the inputs only the slicing machine has. See
        /// <see cref="SlicerGcodeOptions"/>.</param>
        /// <remarks>
        /// <para>Never touches the file system or the network: <c>{{models}}</c> comes from
        /// <see cref="SlicerGcodeOptions.Models"/> and <c>{{modified_settings}}</c> from
        /// <see cref="SlicerGcodeOptions.UserPresets"/>, never from the text or a config directory.</para>
        /// <para>Never throws for any text or list contents: missing or malformed values are left out, and if parsing
        /// fails outright the result has slicer and version <c>Unknown</c> and an empty note. The work is linear in
        /// the size of the input; callers should still cap what they accept. At most 1,000 models are used and model
        /// names are cut to 256 characters.</para>
        /// </remarks>
        /// <exception cref="ArgumentNullException"><paramref name="gcode"/> or <paramref name="options"/> is null.</exception>
        /// <exception cref="ArgumentException"><see cref="SlicerGcodeOptions.Template"/> is not <c>default</c> or
        /// <c>full</c>.</exception>
        public static SlicerGcodeResult Parse(string gcode, SlicerGcodeOptions options)
        {
            ArgumentNullException.ThrowIfNull(gcode);
            ArgumentNullException.ThrowIfNull(options);

            bool full = string.Equals(options.Template, "full", StringComparison.OrdinalIgnoreCase);
            if (!full && !string.Equals(options.Template, "default", StringComparison.OrdinalIgnoreCase))
            {
                throw new ArgumentException($"Template must be \"default\" or \"full\", not \"{options.Template}\".", nameof(options));
            }

            var debugLog = options.DebugLog;
            Action<string> log = message =>
            {
                try
                {
                    debugLog?.Invoke(message);
                }
                catch
                {
                    // A diagnostics sink that fails must not fail the parse.
                }
            };

            try
            {
                var slicer = SlicerDetector.Detect(gcode);
                var template = (full ? slicer.FullTemplate : slicer.DefaultTemplate).getNoteTemplate();
                var parser = slicer.CreateParserBase(template);

                // Both in-memory sources are always set, so no placeholder opens the "full file" or a config root.
                var parseOptions = new ParseOptions(() => Stream.Null, log)
                {
                    Models = Bounded(options.Models),
                    UserPresets = options.UserPresets?.Where(file => file != null).ToList() ?? [],
                };

                var dto = parser.ParseGcode(gcode, parseOptions);
                ApplyOutputName(dto.settings, options.OutputName);
                return new SlicerGcodeResult(dto.Slicer, dto.CuraVersion, slicer.IsRecognized, Serialize(dto.settings));
            }
            catch (Exception e)
            {
                log($"Could not parse the G-code: {e}");
                return Empty(options.OutputName, log);
            }
        }

        private static List<ModelBounds> Bounded(IReadOnlyList<ModelBounds>? models) =>
            (models ?? [])
                .Where(model => model != null)
                .Take(MaxModels)
                .Select(model => model.Name is { Length: > MaxModelNameLength } name ? model with { Name = Truncate(name, MaxModelNameLength) } : model)
                .ToList();

        /// <summary>The <c>file_name</c> and <c>print_name</c> <c>PrintMetadata.Apply</c> sets in the uploader.</summary>
        private static void ApplyOutputName(CuraSettings settings, string? outputName)
        {
            if (string.IsNullOrEmpty(outputName))
            {
                return;
            }

            settings.file_name = PrintName.FileName(outputName);
            settings.print_name = PrintName.Title(Path.GetFileNameWithoutExtension(settings.file_name), CultureInfo.InvariantCulture);
        }

        private static SlicerGcodeResult Empty(string? outputName, Action<string> log)
        {
            var settings = new CuraSettings();
            try
            {
                ApplyOutputName(settings, outputName);
                return new SlicerGcodeResult("Unknown", "Unknown", false, Serialize(settings));
            }
            catch (Exception e)
            {
                log($"Could not use the output name: {e}");
                return new SlicerGcodeResult("Unknown", "Unknown", false, Serialize(new CuraSettings()));
            }
        }

        /// <summary>Keeps a surrogate pair whole, so the cut never leaves half a character.</summary>
        private static string Truncate(string text, int length) =>
            text[..(char.IsHighSurrogate(text[length - 1]) ? length - 1 : length)];

        private static string Serialize(CuraSettings settings) => JsonSerializer.Serialize(settings, JsonContext.Default.CuraSettings);
    }

    /// <summary>
    /// What <see cref="SlicerGcodeParser.Parse"/> needs besides the G-code text.
    /// </summary>
    /// <param name="Template">
    /// The slicer's built-in note template to render: <c>default</c> or <c>full</c> (any case), the uploader's
    /// <c>--default</c> and <c>--full</c>.
    /// </param>
    /// <param name="Models">
    /// Every object instance on the plate with its extrusion bounding box, as the uploader's toolpath scan measures it
    /// over the whole file; used for <c>{{models}}</c> instead of scanning the text. Empty (or null) renders no models
    /// section.
    /// </param>
    /// <param name="UserPresets">
    /// The user's own preset files from the slicer's config directory; used for <c>{{modified_settings}}</c> instead
    /// of reading a config directory. Only files whose kind and name match a user preset the G-code names
    /// (<c>print_settings_id</c>, <c>filament_settings_id</c>, <c>printer_settings_id</c>) are read; others are
    /// ignored. When one of those presets has no file, or more than one, the section lists every changed setting
    /// without splitting saved from unsaved, as the uploader does when it cannot find the files.
    /// </param>
    public sealed record SlicerGcodeOptions(string Template, IReadOnlyList<ModelBounds> Models, IReadOnlyList<UserPresetFile> UserPresets)
    {
        /// <summary>
        /// Optional: the path or file name the slicer wrote the G-code to. When set, <c>file_name</c> is its last
        /// segment ('/' and '\' both separate) and <c>print_name</c> the title the uploader derives from it, e.g.
        /// <c>Benchy Final</c> for <c>C:\Prints\benchy_final.gcode</c>. When null or empty both are null.
        /// </summary>
        public string? OutputName { get; init; }

        /// <summary>
        /// Optional: receives diagnostics, such as why a computed note section is empty or why the parse degraded.
        /// Exceptions it throws are ignored.
        /// </summary>
        public Action<string>? DebugLog { get; init; }
    }

    /// <summary>
    /// One object instance's extrusion bounding box in bed coordinates, in millimeters: the centerline of the model's
    /// extrusions (brim, skirt, supports and towers excluded), not yet widened by the line width. The note adds one
    /// outer-wall line width to X and Y.
    /// </summary>
    /// <param name="Name">The object's name from the slicer's <c>; printing object</c> marker, e.g. <c>3DBenchy.stl</c>.</param>
    /// <param name="MinX">Smallest X reached.</param>
    /// <param name="MaxX">Largest X reached.</param>
    /// <param name="MinY">Smallest Y reached.</param>
    /// <param name="MaxY">Largest Y reached.</param>
    /// <param name="MaxZ">The Z of the last layer that extruded for this object, i.e. its printed height.</param>
    public sealed record ModelBounds(string Name, double MinX, double MaxX, double MinY, double MaxY, double MaxZ);

    /// <summary>
    /// One of the user's preset files, as the slicer saved it under <c>user/&lt;account&gt;/&lt;kind&gt;/&lt;name&gt;.json</c>.
    /// </summary>
    /// <param name="Kind">The preset folder: <c>process</c>, <c>filament</c> or <c>machine</c>.</param>
    /// <param name="Name">The preset name, which is the file name without <c>.json</c>. Compared exactly.</param>
    /// <param name="Json">The file's contents. A file that is not a JSON object counts as unreadable.</param>
    public sealed record UserPresetFile(string Kind, string Name, string Json);

    /// <summary>
    /// What <see cref="SlicerGcodeParser.Parse"/> found. Immutable.
    /// </summary>
    public sealed class SlicerGcodeResult
    {
        internal SlicerGcodeResult(string slicer, string version, bool isRecognized, string settingsJson)
        {
            Slicer = slicer;
            Version = version;
            IsRecognized = isRecognized;
            SettingsJson = settingsJson;
        }

        /// <summary>
        /// The slicer, named as the uploader reports it: <c>OrcaSlicer</c>, <c>PrusaSlicer</c>, <c>BambuStudioSlicer</c>,
        /// <c>FLSunSlicer</c> or <c>AnycubicSlicerNext</c>; <c>Unknown</c> only when parsing failed outright.
        /// </summary>
        public string Slicer { get; }

        /// <summary>The slicer's version from the header, or <c>Unknown</c> when the text has none.</summary>
        public string Version { get; }

        /// <summary>
        /// True when the slicer's marker identified it. False when it was chosen by how well each slicer's template
        /// matches the settings, which is a best guess.
        /// </summary>
        public bool IsRecognized { get; }

        /// <summary>
        /// The <c>settings</c> object the uploader posts for the same file, as indented JSON with the uploader's
        /// property names: <c>note</c>, <c>print_name</c>, <c>estimated_print_time_seconds</c>,
        /// <c>material_used_mg</c>, <c>snapshot</c> (base64 thumbnail), <c>file_name</c> and <c>filamentUsage</c>.
        /// Absent values are <c>null</c>, as in the uploader's request; <c>print_name</c> and <c>file_name</c> are
        /// null unless <see cref="SlicerGcodeOptions.OutputName"/> was given.
        /// </summary>
        public string SettingsJson { get; }
    }
}
