namespace Slic3rPostProcessingUploader.Services.Parsers
{
    internal interface IGcodeParser
    {
        /// <summary>
        /// Parses G-code that is entirely in memory. Computed placeholders that need the full toolpath read it from
        /// <paramref name="gcode"/> itself.
        /// </summary>
        CuraSettingDto ParseGcode(string gcode);

        /// <summary>
        /// Parses the (possibly windowed) G-code text, with <paramref name="options"/> supplying access to the complete
        /// file for computed placeholders.
        /// </summary>
        CuraSettingDto ParseGcode(string gcode, ParseOptions options);
    }
}
