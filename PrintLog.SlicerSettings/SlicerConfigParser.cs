namespace PrintLog.SlicerSettings
{
    /// <summary>
    /// Parses slicer settings from text a connector or server has, rather than from a whole G-code file: the settings
    /// block a slicer writes, plus its header when available.
    /// </summary>
    public static class SlicerConfigParser
    {
        /// <summary>
        /// Detects the slicer, reads every <c>; key = value</c> setting, and renders the slicer's built-in default note
        /// exactly as the uploader would for the same file with <c>--default</c>.
        /// </summary>
        /// <param name="configBlock">
        /// The settings the slicer wrote into the G-code, as text. For OrcaSlicer, Anycubic Slicer Next and PrusaSlicer
        /// this is the run of comment lines after the last toolpath command (OrcaSlicer: from
        /// <c>; EXECUTABLE_BLOCK_END</c>; PrusaSlicer: the <c>; filament used</c> totals through
        /// <c>; prusaslicer_config = end</c>). Bambu Studio and FLSun Slicer write the block near the top instead: everything after
        /// <c>; HEADER_BLOCK_END</c> through <c>; CONFIG_BLOCK_END</c> (FLSun writes its time and filament totals just
        /// before <c>; CONFIG_BLOCK_START</c>). Toolpaths may be included but are never needed.
        /// </param>
        /// <param name="header">
        /// Optional: the start of the file through <c>; HEADER_BLOCK_END</c> (PrusaSlicer: its first line). It carries
        /// the slicer's name and version, and for Bambu Studio the print time and filament totals. Without it the
        /// version is <c>Unknown</c> and the slicer is guessed from the settings alone (see
        /// <see cref="SlicerConfigResult.IsRecognized"/>); OrcaSlicer forks such as Anycubic Slicer Next then read as
        /// OrcaSlicer, because nothing in their settings block tells them apart. Snapmaker Orca has no detection yet and
        /// always reads as OrcaSlicer with version <c>Unknown</c>.
        /// </param>
        /// <param name="debugLog">
        /// Optional: receives diagnostics, such as why a computed note section is empty or why the parse degraded.
        /// Exceptions it throws are ignored.
        /// </param>
        /// <remarks>
        /// <para>Never touches the file system or the network. Never throws for any text: missing or malformed values
        /// are left out (totals are null, never 0 or negative), and if parsing fails outright the result has
        /// slicer and version <c>Unknown</c>, no settings and an empty note.
        /// The work is linear in the length of the text.</para>
        /// <para>Callers should cap what they accept: a settings block is tens of kilobytes. Text (header and block
        /// together) over 4,000,000 characters is windowed like a large G-code file, keeping only its first and last
        /// 2,000,000 characters, so settings in the middle are not read.</para>
        /// <para>Computed note sections degrade as follows. <c>{{filament_profiles}}</c> is complete.
        /// <c>{{modified_settings}}</c> lists every setting that differs from the system profile, but cannot say which
        /// of those were saved in the user's own presets, because those files live on the slicer's machine.
        /// <c>{{models}}</c> is always empty: object sizes are measured from the toolpaths.</para>
        /// <para>Thumbnails are not extracted.</para>
        /// </remarks>
        /// <exception cref="ArgumentNullException"><paramref name="configBlock"/> is null.</exception>
        public static SlicerConfigResult Parse(string configBlock, string? header = null, Action<string>? debugLog = null)
        {
            ArgumentNullException.ThrowIfNull(configBlock);

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
                // Header first, so "first match wins" lookups see the text in the order the file has it.
                var text = string.IsNullOrEmpty(header) ? configBlock : string.Concat(header, "\n", configBlock);

                var slicer = SlicerDetector.Detect(text);
                var parser = slicer.CreateParserBase(slicer.DefaultTemplate.getNoteTemplate());

                // The "full file" a computed placeholder may stream is this same text, and there are no slicer config
                // roots, so nothing on this machine is read.
                var inMemory = ParseOptions.InMemory(text);
                return parser.ParseConfig(text, new ParseOptions(inMemory.OpenFullGcode, log), slicer.IsRecognized);
            }
            catch (Exception e)
            {
                log($"Could not parse the slicer settings: {e}");
                return SlicerConfigResult.Empty;
            }
        }
    }

    /// <summary>
    /// What <see cref="SlicerConfigParser.Parse"/> found. Immutable.
    /// </summary>
    public sealed class SlicerConfigResult
    {
        internal static readonly SlicerConfigResult Empty = new(
            "Unknown", "Unknown", false, new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase).AsReadOnly(), string.Empty, null, null, null, []);

        internal SlicerConfigResult(
            string slicer,
            string version,
            bool isRecognized,
            IReadOnlyDictionary<string, string> settings,
            string note,
            int? estimatedPrintTimeSeconds,
            int? totalFilamentMg,
            double? totalFilamentLengthMm,
            IReadOnlyList<FilamentEstimate> filaments)
        {
            Slicer = slicer;
            Version = version;
            IsRecognized = isRecognized;
            Settings = settings;
            Note = note;
            EstimatedPrintTimeSeconds = estimatedPrintTimeSeconds;
            TotalFilamentMg = totalFilamentMg;
            TotalFilamentLengthMm = totalFilamentLengthMm;
            Filaments = filaments.ToArray().AsReadOnly();
        }

        /// <summary>
        /// The slicer, named as the uploader reports it: <c>OrcaSlicer</c>, <c>PrusaSlicer</c>, <c>BambuStudioSlicer</c>,
        /// <c>FLSunSlicer</c> or <c>AnycubicSlicerNext</c>; <c>Unknown</c> only when parsing failed outright.
        /// </summary>
        public string Slicer { get; }

        /// <summary>The slicer's version from the header, or <c>Unknown</c> when no header was given.</summary>
        public string Version { get; }

        /// <summary>
        /// True when the slicer's marker (in the header) identified it. False when it was chosen by how well each
        /// slicer's template matches the settings, which is a best guess.
        /// </summary>
        public bool IsRecognized { get; }

        /// <summary>
        /// Every <c>; key = value</c> setting in the text, e.g. <c>print_settings_id</c>, <c>printer_settings_id</c>,
        /// <c>filament_settings_id</c>, <c>layer_height</c>. Keys compare case-insensitively; when a key repeats, the
        /// first non-empty value wins. Settings with an empty value are left out.
        /// </summary>
        public IReadOnlyDictionary<string, string> Settings { get; }

        /// <summary>The slicer's built-in default note template, rendered.</summary>
        public string Note { get; }

        /// <summary>The slicer's print time estimate in seconds, or null when the text has none.</summary>
        public int? EstimatedPrintTimeSeconds { get; }

        /// <summary>
        /// Total filament weight in milligrams: the sum of the slicer's per-slot grams when it wrote them, otherwise
        /// estimated from the first slot's length, diameter and density. Null when neither is possible.
        /// </summary>
        public int? TotalFilamentMg { get; }

        /// <summary>Total filament length in millimetres over every slot, or null when the text has none.</summary>
        public double? TotalFilamentLengthMm { get; }

        /// <summary>Each slot the print uses, in slot order. Empty when the text has no per-slot lengths.</summary>
        public IReadOnlyList<FilamentEstimate> Filaments { get; }
    }

    /// <summary>
    /// One slot's share of a print, from the slicer's per-slot <c>filament used [mm]</c> list.
    /// </summary>
    /// <param name="SlotIndex">The slot (tool) index, from 0.</param>
    /// <param name="LengthMm">The slicer's length estimate for this slot, in millimetres. Always positive.</param>
    /// <param name="Colour">The slot's colour as the slicer wrote it (usually <c>#RRGGBB</c>), or null.</param>
    /// <param name="Type">The slot's material type (<c>PLA</c>, <c>PETG</c>, ...), or null.</param>
    /// <param name="Description">The uploader's note for the slot, e.g. <c>Slot 1 · Red (#E72F1D) · PLA</c>.</param>
    public sealed record FilamentEstimate(int SlotIndex, double LengthMm, string? Colour, string? Type, string Description);
}
