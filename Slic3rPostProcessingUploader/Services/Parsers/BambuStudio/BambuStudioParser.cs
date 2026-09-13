namespace Slic3rPostProcessingUploader.Services.Parsers.BambuStudio
{
    internal class BambuStudioParser(string? noteTemplate) : GcodeParserBase(noteTemplate)
    {
        protected override string SlicerName => "BambuStudioSlicer";
        protected override string SlicerVersionPattern => @"; BambuStudio (.+)\s";
        protected override INoteTemplate CreateDefaultTemplate() => new BambuStudioDefaultNoteTemplate();
        protected override ReadOnlySpan<char> SettingSeparators => "=:";
        protected override string FilamentLengthKey => "total filament length [mm]";
        protected override string FilamentWeightKey => "total filament weight [g]";
        protected override bool SupportsMultiFilament => true;

        public static bool IsBambuStudio(string gcode)
        {
            return gcode.Contains("; BambuStudio ", StringComparison.Ordinal);
        }
    }
}
