using Slic3rPostProcessingUploader.Services.Installer;
using System.Runtime.InteropServices;
using System.Text.Json.Nodes;

namespace Slic3rPostProcessingUploader.Services.Installer.OrcaFamily
{
    internal abstract class OrcaFamilyProfileInstaller : ISlicerProfileInstaller
    {
        private readonly string? _configRootOverride;

        public abstract string SlicerName { get; }
        protected abstract string SlicerDirectoryName { get; }

        protected OrcaFamilyProfileInstaller(string? configRootOverride = null)
        {
            _configRootOverride = configRootOverride;
        }

        protected string GetConfigRoot()
        {
            if (_configRootOverride != null)
                return _configRootOverride;

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                return Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                    SlicerDirectoryName);

            if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                return Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.Personal),
                    "Library", "Application Support", SlicerDirectoryName);

            // Linux: respect XDG_CONFIG_HOME
            string? xdg = Environment.GetEnvironmentVariable("XDG_CONFIG_HOME");
            string baseDir = !string.IsNullOrEmpty(xdg)
                ? xdg
                : Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Personal), ".config");
            return Path.Combine(baseDir, SlicerDirectoryName);
        }

        // Exposed for testing only
        internal string GetConfigRootForTesting() => GetConfigRoot();

        public bool IsDetected() => Directory.Exists(GetConfigRoot());

        public SlicerInstallStatus GetInstallStatus(string executablePath) => throw new NotImplementedException();
        public InstallResult Install(string executablePath, string flags, bool dryRun) => throw new NotImplementedException();
        public InstallResult Uninstall(string executablePath, bool dryRun) => throw new NotImplementedException();

        internal record SystemProfile(string Name, string FilePath, string Version);

        protected List<SystemProfile> FindSelectableSystemProfiles()
        {
            var result = new List<SystemProfile>();
            var systemDir = Path.Combine(GetConfigRoot(), "system");
            if (!Directory.Exists(systemDir)) return result;

            foreach (var file in Directory.EnumerateFiles(systemDir, "*.json", SearchOption.AllDirectories))
            {
                // Only files in a "process" subdirectory
                if (!file.Contains(Path.DirectorySeparatorChar + "process" + Path.DirectorySeparatorChar))
                    continue;

                try
                {
                    var text = File.ReadAllText(file);
                    var node = JsonNode.Parse(text);
                    if (node == null) continue;

                    var instantiation = node["instantiation"]?.GetValue<string>();
                    if (instantiation != "true") continue;

                    var name = node["name"]?.GetValue<string>();
                    var version = node["version"]?.GetValue<string>() ?? "";
                    if (string.IsNullOrEmpty(name)) continue;

                    result.Add(new SystemProfile(name, file, version));
                }
                catch { /* skip malformed JSON */ }
            }

            return result;
        }

        protected List<string> FindUserAccountDirs()
        {
            var userDir = Path.Combine(GetConfigRoot(), "user");
            if (!Directory.Exists(userDir)) return new List<string>();
            return Directory.GetDirectories(userDir).ToList();
        }

        // Exposed for testing only
        internal List<SystemProfile> FindSelectableSystemProfilesForTesting() => FindSelectableSystemProfiles();
        internal List<string> FindUserAccountDirsForTesting() => FindUserAccountDirs();
    }
}
