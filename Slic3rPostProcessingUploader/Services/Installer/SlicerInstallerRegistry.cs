using Slic3rPostProcessingUploader.Services.Installer.OrcaFamily;

namespace Slic3rPostProcessingUploader.Services.Installer
{
    internal static class SlicerInstallerRegistry
    {
        /// <summary>
        /// All supported slicer installers. Add new slicers here.
        /// </summary>
        public static IReadOnlyList<ISlicerProfileInstaller> All =>
        [
            new OrcaSlicerInstaller(),
            new SnapmakerOrcaInstaller(),
            new AnycubicSlicerNextInstaller(),
        ];

        /// <summary>
        /// The config directory of every Orca-family slicer on this machine, whether or not it exists. The parser uses
        /// these to look up the user's preset files, so the list cannot drift from the installers.
        /// </summary>
        public static IEnumerable<string> OrcaFamilyConfigRoots() =>
            All.OfType<OrcaFamilyProfileInstaller>()
                .Select(installer => installer.GetConfigRoot())
                .OfType<string>();

        /// <summary>
        /// Every config directory the parsers may search for the user's own presets: the Orca family's, plus Bambu
        /// Studio (an Orca-family layout without an installer) and PrusaSlicer 2.x (its own .ini layout; alpha and
        /// beta builds keep a directory of their own). Each preset locator only recognises its own layout, so one
        /// list serves all of them. PrusaSlicer 3.0 stores presets as YAML under a "PrusaSlicer3-dev" directory,
        /// which no locator reads yet, and a custom --datadir cannot be discovered.
        /// </summary>
        public static IEnumerable<string> PresetConfigRoots() =>
            OrcaFamilyConfigRoots()
                .Concat(new[] { "BambuStudio", "PrusaSlicer", "PrusaSlicer-alpha", "PrusaSlicer-beta" }.Select(SlicerConfigRoot.ForCurrentMachine).OfType<string>())
                .Distinct(StringComparer.Ordinal);
    }
}
