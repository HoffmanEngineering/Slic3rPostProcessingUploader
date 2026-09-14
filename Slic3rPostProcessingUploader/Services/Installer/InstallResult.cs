namespace Slic3rPostProcessingUploader.Services.Installer
{
    /// <param name="Created">New user override files created.</param>
    /// <param name="Updated">Existing installer-owned overrides updated (our entry added, or a stale path/flags replaced).</param>
    /// <param name="Skipped">Installer-owned overrides already up to date.</param>
    /// <param name="WithOtherScripts">Profiles that had other post-process scripts; ours was appended alongside.</param>
    /// <param name="RemovedFiles">Uninstall only: installer-owned override files deleted entirely.</param>
    /// <param name="ModifiedFiles">Uninstall only: installer-owned files kept (they carry other settings) but our entry removed.</param>
    /// <param name="CoveredByHandMade">Install only: system profiles skipped because a profile the user made by hand already runs the uploader.</param>
    /// <param name="HandMadeRefreshed">Install only: hand-made profiles whose uploader path was stale and has been pointed at the current executable (their flags are kept).</param>
    /// <param name="HandMadeLeft">Uninstall only: hand-made profiles that still reference the uploader; the installer never edits or deletes those.</param>
    /// <param name="Unreadable">User profile files that could not be parsed and were left alone.</param>
    internal record InstallResult(
        int Created,
        int Updated,
        int Skipped,
        int WithOtherScripts,
        int RemovedFiles,
        int ModifiedFiles,
        int CoveredByHandMade = 0,
        int HandMadeRefreshed = 0,
        int HandMadeLeft = 0,
        int Unreadable = 0
    );
}
