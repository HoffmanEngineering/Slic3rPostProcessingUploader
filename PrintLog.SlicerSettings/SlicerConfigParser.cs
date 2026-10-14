namespace PrintLog.SlicerSettings
{
    /// <summary>
    /// Parses slicer settings from text a connector or server has, rather than from a whole G-code file: the settings
    /// block a slicer writes, plus its header when available.
    /// </summary>
    public static class SlicerConfigParser
    {
        /// <summary>
        /// Detects the slicer, reads every <c>; key = value</c> setting and renders the slicer's built-in default note,
        /// exactly as the uploader would for the same file with <c>--default</c>.
        /// </summary>
        /// <param name="configBlock">
        /// The settings the slicer wrote into the G-code, as text. For OrcaSlicer, Anycubic Slicer Next and PrusaSlicer
        /// this is the run of comment lines after the last toolpath command (OrcaSlicer: from
        /// <c>; EXECUTABLE_BLOCK_END</c>; PrusaSlicer: the <c>; filament used</c> totals through
        /// <c>; prusaslicer_config = end</c>). Bambu Studio writes the block near the top instead, from
        /// <c>; CONFIG_BLOCK_START</c> to <c>; CONFIG_BLOCK_END</c>. Toolpaths may be included but are never needed.
        /// </param>
        /// <param name="header">
        /// Optional: the start of the file through <c>; HEADER_BLOCK_END</c> (PrusaSlicer: its first line). It carries
        /// the slicer's name and version, and for Bambu Studio the print time and filament totals. Without it the
        /// version is <c>Unknown</c> and the slicer is guessed from the settings alone (see
        /// <see cref="SlicerConfigResult.IsRecognized"/>); OrcaSlicer forks such as Anycubic Slicer Next then read as
        /// OrcaSlicer, because nothing in their settings block tells them apart.
        /// </param>
        /// <param name="debugLog">Optional: receives diagnostics, such as why a computed note section is empty.</param>
        /// <remarks>
        /// <para>Never touches the file system or the network, and never throws on malformed or empty text: missing
        /// settings render as blanks and an unidentifiable block falls back to OrcaSlicer.</para>
        /// <para>Computed note sections degrade as follows. <c>{{filament_profiles}}</c> is complete.
        /// <c>{{modified_settings}}</c> lists every setting that differs from the system profile, but cannot say which
        /// of those were saved in the user's own presets, because those files live on the slicer's machine.
        /// <c>{{models}}</c> is always empty: object sizes are measured from the toolpaths.</para>
        /// <para>Thumbnails are in the header region, so <see cref="CuraSettings.Snapshot"/> is set only when the header
        /// passed in includes them.</para>
        /// </remarks>
        /// <exception cref="ArgumentNullException"><paramref name="configBlock"/> is null.</exception>
        public static SlicerConfigResult Parse(string configBlock, string? header = null, Action<string>? debugLog = null)
        {
            ArgumentNullException.ThrowIfNull(configBlock);

            // Header first, so "first match wins" lookups see the text in the order the file has it.
            var text = string.IsNullOrEmpty(header) ? configBlock : string.Concat(header, "\n", configBlock);

            var slicer = SlicerDetector.Detect(text);
            var parser = slicer.CreateParserBase(slicer.DefaultTemplate.getNoteTemplate());

            // InMemory: the "full file" a computed placeholder may stream is this same text, and there are no slicer
            // config roots, so nothing on this machine is read.
            var inMemory = ParseOptions.InMemory(text);
            var options = new ParseOptions(inMemory.OpenFullGcode, debugLog ?? (_ => { }));
            var dto = parser.ParseGcode(text, options);

            var settings = GcodeSettings.Parse(GcodeWindow.Trim(text), parser.SettingSeparators).ToReadOnlyDictionary();
            return new SlicerConfigResult(dto, settings, slicer.IsRecognized);
        }
    }

    /// <summary>
    /// What <see cref="SlicerConfigParser.Parse"/> found.
    /// </summary>
    public sealed class SlicerConfigResult
    {
        internal SlicerConfigResult(CuraSettingDto dto, IReadOnlyDictionary<string, string> settings, bool isRecognized)
        {
            Dto = dto;
            Settings = settings;
            IsRecognized = isRecognized;
        }

        /// <summary>
        /// The slicer, as the uploader reports it: <c>OrcaSlicer</c>, <c>PrusaSlicer</c>, <c>BambuStudioSlicer</c>,
        /// <c>FLSunSlicer</c> or <c>AnycubicSlicerNext</c>.
        /// </summary>
        public string Slicer => Dto.Slicer;

        /// <summary>The slicer's version from the header, or <c>Unknown</c> when no header was given.</summary>
        public string Version => Dto.CuraVersion;

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
        public string Note => Dto.settings.note;

        /// <summary>
        /// The full parse, in the shape the uploader posts to <c>/api/Cura/settings</c>: print time estimate, filament
        /// usage, note and (when the header carried one) the thumbnail. <see cref="CuraSettingDto.PluginVersion"/>,
        /// <see cref="CuraSettings.print_name"/> and <see cref="CuraSettings.file_name"/> are left unset.
        /// </summary>
        public CuraSettingDto Dto { get; }
    }
}
