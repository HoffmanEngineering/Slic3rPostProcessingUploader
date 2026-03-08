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
    }
}
