using System.Text.RegularExpressions;

namespace Slic3rPostProcessingUploader.Services.Parsers.BambuStudio
{
    internal partial class BambuStudioParser(string? noteTemplate) : GcodeParserBase(noteTemplate)
    {
        protected override string SlicerName => "BambuStudioSlicer";
        [GeneratedRegex(@"; BambuStudio (.+)\s", RegexOptions.IgnoreCase | RegexOptions.Multiline)]
        private static partial Regex VersionRegex();
        protected override Regex SlicerVersionRegex => VersionRegex();
        protected override INoteTemplate CreateDefaultTemplate() => EmbeddedNoteTemplate.Default("BambuStudio");
        protected internal override ReadOnlySpan<char> SettingSeparators => "=:";
        protected override string FilamentLengthKey => "total filament length [mm]";
        protected override string FilamentWeightKey => "total filament weight [g]";
        protected override bool SupportsMultiFilament => true;

        public static bool IsBambuStudio(string gcode)
        {
            return gcode.Contains("; BambuStudio ", StringComparison.Ordinal);
        }
    }
}
