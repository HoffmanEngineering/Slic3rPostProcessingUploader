namespace Slic3rPostProcessingUploader.Services.Installer
{
    /// <param name="IsInstalled">True if any profiles already contain our script.</param>
    /// <param name="ProfileCount">Total selectable process profiles found.</param>
    /// <param name="InstalledCount">Profiles already containing our script.</param>
    /// <param name="InstalledFlags">Flags detected in existing install, e.g. "--full".</param>
    internal record SlicerInstallStatus(
        bool IsInstalled,
        int ProfileCount,
        int InstalledCount,
        string? InstalledFlags
    );
}
