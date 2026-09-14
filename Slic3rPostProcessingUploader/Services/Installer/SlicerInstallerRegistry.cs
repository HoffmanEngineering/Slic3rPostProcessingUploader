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
    }
}
