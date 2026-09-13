using System.Runtime.InteropServices;

namespace Slic3rPostProcessingUploader.Services.Installer
{
    /// <summary>
    /// Where an OrcaSlicer-family slicer keeps its config tree on each platform.
    /// </summary>
    internal static class SlicerConfigRoot
    {
        public static string? ForCurrentMachine(string slicerDirectoryName)
        {
            var platform = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? OSPlatform.Windows
                : RuntimeInformation.IsOSPlatform(OSPlatform.OSX) ? OSPlatform.OSX
                : OSPlatform.Linux;

            // UserProfile is the home directory on every platform. SpecialFolder.Personal is NOT: on Unix it means
            // ~/Documents, and .NET returns "" when that folder is missing, which would turn the result into a
            // relative path resolved against the current directory.
            return Resolve(
                platform,
                slicerDirectoryName,
                appData: Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                home: Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                xdgConfigHome: Environment.GetEnvironmentVariable("XDG_CONFIG_HOME"));
        }

        /// <returns>The config root, or null when the platform's base directory is unknown.</returns>
        public static string? Resolve(OSPlatform platform, string slicerDirectoryName, string? appData, string? home, string? xdgConfigHome)
        {
            if (platform == OSPlatform.Windows)
                return string.IsNullOrWhiteSpace(appData) ? null : Path.Combine(appData, slicerDirectoryName);

            if (platform == OSPlatform.OSX)
                return UnixPath(home, "Library", "Application Support", slicerDirectoryName);

            return !string.IsNullOrWhiteSpace(xdgConfigHome)
                ? UnixPath(xdgConfigHome, slicerDirectoryName)
                : UnixPath(home, ".config", slicerDirectoryName);
        }

        // Joined with '/' rather than Path.Combine so the Unix layouts are testable on any OS.
        private static string? UnixPath(string? baseDir, params string[] segments) =>
            string.IsNullOrWhiteSpace(baseDir) ? null : string.Join('/', [baseDir.TrimEnd('/'), .. segments]);
    }
}
