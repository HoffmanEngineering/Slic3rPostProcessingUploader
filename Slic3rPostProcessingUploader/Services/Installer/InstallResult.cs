namespace Slic3rPostProcessingUploader.Services.Installer
{
    /// <param name="Created">New user override files created.</param>
    /// <param name="Updated">Existing user overrides updated.</param>
    /// <param name="Skipped">Profiles already up to date.</param>
    /// <param name="WithOtherScripts">Profiles that had other post-process scripts; ours was appended alongside.</param>
    /// <param name="RemovedFiles">Uninstall only: user override files deleted entirely.</param>
    /// <param name="ModifiedFiles">Uninstall only: files kept but our entry removed.</param>
    internal record InstallResult(
        int Created,
        int Updated,
        int Skipped,
        int WithOtherScripts,
        int RemovedFiles,
        int ModifiedFiles
    );
}
