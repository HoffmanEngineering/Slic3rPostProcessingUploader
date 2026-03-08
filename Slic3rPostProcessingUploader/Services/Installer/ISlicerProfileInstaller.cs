namespace Slic3rPostProcessingUploader.Services.Installer
{
    internal interface ISlicerProfileInstaller
    {
        /// <summary>Display name shown to the user, e.g. "OrcaSlicer"</summary>
        string SlicerName { get; }

        /// <summary>Returns true if the slicer's config directory exists on this machine.</summary>
        bool IsDetected();

        /// <summary>Scans installed profiles to determine current installation state.</summary>
        SlicerInstallStatus GetInstallStatus(string executablePath);

        /// <summary>Injects the executable path into all selectable process profiles.</summary>
        InstallResult Install(string executablePath, string flags, bool dryRun);

        /// <summary>Removes the executable path from all process profiles.</summary>
        InstallResult Uninstall(string executablePath, bool dryRun);
    }
}
