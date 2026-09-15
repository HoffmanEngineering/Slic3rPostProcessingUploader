using System.Text.RegularExpressions;
using Slic3rPostProcessingUploader.Services.Parsers.Computed;

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

        // Bambu Studio writes the same profile and different_settings_to_system keys as Orca (without inherits_group,
        // so changes are listed flat) and labels objects on every layer, though only by id.
        protected override IReadOnlyList<ComputedPlaceholder> ComputedPlaceholders =>
            [FilamentProfilesPlaceholder.Instance, ModifiedSettingsPlaceholder.Instance, ModelsPlaceholder.Instance];

        public static bool IsBambuStudio(string gcode)
        {
            return gcode.Contains("; BambuStudio ", StringComparison.Ordinal);
        }
    }
}
