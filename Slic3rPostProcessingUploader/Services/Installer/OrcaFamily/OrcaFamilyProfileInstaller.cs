using Slic3rPostProcessingUploader.Services.Installer;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace Slic3rPostProcessingUploader.Services.Installer.OrcaFamily
{
    internal abstract class OrcaFamilyProfileInstaller : ISlicerProfileInstaller
    {
        private const string ProfileNameSuffix = " - 3DPrintLog";
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

        public SlicerInstallStatus GetInstallStatus(string executablePath)
        {
            var systemProfiles = FindSelectableSystemProfiles();
            var userAccountDirs = FindUserAccountDirs();

            int totalProfiles = systemProfiles.Count;
            int installedCount = 0;
            string? detectedFlags = null;

            foreach (var systemProfile in systemProfiles)
            {
                foreach (var accountDir in userAccountDirs)
                {
                    var overridePath = Path.Combine(accountDir, "process", systemProfile.Name + ProfileNameSuffix + ".json");
                    if (!File.Exists(overridePath)) continue;

                    var node = JsonNode.Parse(File.ReadAllText(overridePath));
                    var postProcess = node?["post_process"]?.AsArray();
                    if (postProcess == null) continue;

                    var ourEntry = postProcess
                        .Select(e => e?.GetValue<string>())
                        .FirstOrDefault(e => e?.Contains("Slic3rPostProcessingUploader") == true);

                    if (ourEntry != null)
                    {
                        installedCount++;
                        if (detectedFlags == null)
                            detectedFlags = ExtractFlags(ourEntry);
                    }
                }
            }

            return new SlicerInstallStatus(
                IsInstalled: installedCount > 0,
                ProfileCount: totalProfiles,
                InstalledCount: installedCount,
                InstalledFlags: detectedFlags);
        }

        private const string UploaderMarker = "Slic3rPostProcessingUploader";

        private static readonly JsonSerializerOptions WriteOptions = new() { WriteIndented = true };

        public InstallResult Install(string executablePath, string flags, bool dryRun)
        {
            string scriptEntry = BuildScriptEntry(executablePath, flags);
            var systemProfiles = FindSelectableSystemProfiles();
            var userAccountDirs = FindUserAccountDirs();

            int created = 0, updated = 0, skipped = 0, withOtherScripts = 0, coveredByHandMade = 0, handMadeRefreshed = 0, unreadable = 0;

            foreach (var accountDir in userAccountDirs)
            {
                var processDir = Path.Combine(accountDir, "process");
                var handMade = FindHandMadeUploaderProfiles(processDir, ref unreadable);

                // A hand-made profile that already runs the uploader is left in charge of its parent system profile;
                // creating our own override next to it would only show up as a duplicate in the slicer's dropdown.
                foreach (var profile in handMade)
                {
                    if (!PathMatches(profile.ExePath, executablePath))
                    {
                        if (!dryRun)
                        {
                            profile.PostProcess[profile.EntryIndex] = JsonValue.Create(BuildScriptEntry(executablePath, profile.Flags));
                            File.WriteAllText(profile.FilePath, profile.Node.ToJsonString(WriteOptions));
                        }
                        handMadeRefreshed++;
                    }
                }
                var coveredParents = handMade.Select(p => p.Inherits).ToHashSet(StringComparer.Ordinal);

                foreach (var systemProfile in systemProfiles)
                {
                    if (coveredParents.Contains(systemProfile.Name))
                    {
                        coveredByHandMade++;
                        continue;
                    }

                    string overrideName = systemProfile.Name + ProfileNameSuffix;
                    var overridePath = Path.Combine(processDir, overrideName + ".json");

                    if (File.Exists(overridePath))
                    {
                        var node = TryReadObject(overridePath);
                        if (node == null)
                        {
                            unreadable++;
                            continue;
                        }

                        var postProcess = node["post_process"]?.AsArray();
                        int ourIndex = IndexOfOurEntry(postProcess);

                        if (ourIndex >= 0 && postProcess![ourIndex]!.GetValue<string>() == scriptEntry)
                        {
                            skipped++;
                            continue;
                        }

                        if (ourIndex >= 0)
                        {
                            // Stale path or different flags: the slicer would otherwise keep calling an executable that is gone.
                            postProcess![ourIndex] = JsonValue.Create(scriptEntry);
                        }
                        else if (postProcess == null)
                        {
                            node["post_process"] = new JsonArray(JsonValue.Create(scriptEntry));
                        }
                        else
                        {
                            if (postProcess.Count > 0)
                                withOtherScripts++;
                            postProcess.Add(JsonValue.Create(scriptEntry));
                        }

                        if (!dryRun)
                            File.WriteAllText(overridePath, node.ToJsonString(WriteOptions));
                        updated++;
                    }
                    else
                    {
                        if (!dryRun)
                        {
                            Directory.CreateDirectory(processDir);
                            var newOverride = new JsonObject
                            {
                                ["from"] = "User",
                                ["inherits"] = systemProfile.Name,
                                ["name"] = overrideName,
                                ["post_process"] = new JsonArray(JsonValue.Create(scriptEntry)),
                                ["print_settings_id"] = overrideName,
                                ["version"] = string.IsNullOrEmpty(systemProfile.Version) ? "1.0.0.0" : systemProfile.Version
                            };
                            File.WriteAllText(overridePath, newOverride.ToJsonString(WriteOptions));
                        }
                        created++;
                    }
                }
            }

            return new InstallResult(created, updated, skipped, withOtherScripts, 0, 0,
                CoveredByHandMade: coveredByHandMade, HandMadeRefreshed: handMadeRefreshed, Unreadable: unreadable);
        }

        /// <summary>A user process profile the installer did not create, whose post_process already calls the uploader.</summary>
        private sealed record HandMadeProfile(string FilePath, JsonObject Node, JsonArray PostProcess, int EntryIndex, string Inherits, string ExePath, string? Flags);

        private static List<HandMadeProfile> FindHandMadeUploaderProfiles(string processDir, ref int unreadable)
        {
            var result = new List<HandMadeProfile>();
            if (!Directory.Exists(processDir)) return result;

            foreach (var file in Directory.EnumerateFiles(processDir, "*.json").Order(StringComparer.Ordinal))
            {
                if (IsInstallerOwned(file)) continue;

                var node = TryReadObject(file);
                if (node == null)
                {
                    unreadable++;
                    continue;
                }

                var postProcess = node["post_process"]?.AsArray();
                int index = IndexOfOurEntry(postProcess);
                if (index < 0) continue;

                var inherits = node["inherits"]?.GetValue<string>();
                if (string.IsNullOrEmpty(inherits)) continue;

                var (exePath, flags) = SplitScriptEntry(postProcess![index]!.GetValue<string>());
                result.Add(new HandMadeProfile(file, node, postProcess, index, inherits, exePath, flags));
            }

            return result;
        }

        private static bool IsInstallerOwned(string filePath) =>
            Path.GetFileNameWithoutExtension(filePath).EndsWith(ProfileNameSuffix, StringComparison.Ordinal);

        private static JsonObject? TryReadObject(string filePath)
        {
            try
            {
                return JsonNode.Parse(File.ReadAllText(filePath)) as JsonObject;
            }
            catch (Exception e) when (e is JsonException or ArgumentException or IOException)
            {
                // JsonNode reports a duplicated key as ArgumentException rather than JsonException.
                return null;
            }
        }

        private static int IndexOfOurEntry(JsonArray? postProcess)
        {
            if (postProcess == null) return -1;
            for (int i = 0; i < postProcess.Count; i++)
            {
                if (postProcess[i] is JsonValue value && value.TryGetValue<string>(out var entry) && entry.Contains(UploaderMarker))
                    return i;
            }
            return -1;
        }

        private static bool PathMatches(string a, string b) =>
            string.Equals(a, b, OperatingSystem.IsWindows() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal);

        private static string? ExtractFlags(string scriptEntry) => SplitScriptEntry(scriptEntry).Flags;

        /// <summary>
        /// Splits <c>"C:\path\uploader.exe" --flags</c> or <c>C:\path\uploader.exe --flags</c> into the executable
        /// path and the trailing flags (null when there are none).
        /// </summary>
        private static (string ExePath, string? Flags) SplitScriptEntry(string scriptEntry)
        {
            string exePath, remaining;
            if (scriptEntry.StartsWith('"'))
            {
                var closeQuote = scriptEntry.IndexOf('"', 1);
                if (closeQuote < 0) return (scriptEntry.Trim('"'), null);
                exePath = scriptEntry[1..closeQuote];
                remaining = scriptEntry[(closeQuote + 1)..];
            }
            else
            {
                var space = scriptEntry.IndexOf(' ');
                if (space < 0) return (scriptEntry, null);
                exePath = scriptEntry[..space];
                remaining = scriptEntry[space..];
            }

            remaining = remaining.Trim();
            return (exePath, string.IsNullOrWhiteSpace(remaining) ? null : remaining);
        }

        private static string BuildScriptEntry(string executablePath, string? flags)
        {
            bool needsQuotes = executablePath.Contains(' ');
            string quotedPath = needsQuotes ? $"\"{executablePath}\"" : executablePath;
            return string.IsNullOrWhiteSpace(flags) ? quotedPath : $"{quotedPath} {flags.Trim()}";
        }

        private static readonly HashSet<string> MinimalOverrideFields = new()
        {
            "from", "inherits", "name", "print_settings_id", "version", "is_custom_defined"
        };

        public InstallResult Uninstall(string executablePath, bool dryRun)
        {
            int removedFiles = 0, modifiedFiles = 0, handMadeLeft = 0, unreadable = 0;

            foreach (var accountDir in FindUserAccountDirs())
            {
                var processDir = Path.Combine(accountDir, "process");
                if (!Directory.Exists(processDir)) continue;

                foreach (var file in Directory.EnumerateFiles(processDir, "*.json").Order(StringComparer.Ordinal))
                {
                    var node = TryReadObject(file);
                    if (node == null)
                    {
                        unreadable++;
                        continue;
                    }

                    var postProcess = node["post_process"]?.AsArray();
                    if (IndexOfOurEntry(postProcess) < 0) continue;

                    // Only files the installer created are ours to delete or edit; a profile the user built by hand
                    // is reported so they can decide what to do with it.
                    if (!IsInstallerOwned(file))
                    {
                        handMadeLeft++;
                        continue;
                    }

                    for (int i = postProcess!.Count - 1; i >= 0; i--)
                    {
                        if (postProcess[i]?.GetValue<string>().Contains(UploaderMarker) == true)
                            postProcess.RemoveAt(i);
                    }
                    if (postProcess.Count == 0)
                        node.Remove("post_process");

                    bool hasCustomizations = node.Any(kv => !MinimalOverrideFields.Contains(kv.Key));
                    if (!hasCustomizations)
                    {
                        if (!dryRun) File.Delete(file);
                        removedFiles++;
                    }
                    else
                    {
                        if (!dryRun) File.WriteAllText(file, node.ToJsonString(WriteOptions));
                        modifiedFiles++;
                    }
                }
            }

            return new InstallResult(0, 0, 0, 0, removedFiles, modifiedFiles, HandMadeLeft: handMadeLeft, Unreadable: unreadable);
        }

        internal record SystemProfile(string Name, string FilePath, string Version);

        /// <summary>
        /// The process profiles the slicer actually offers: for each vendor under <c>system/</c>, the files named in the
        /// vendor's <c>{Vendor}.json</c> index that are marked <c>instantiation = true</c>. Vendor folders accumulate
        /// leftovers ("… copy.json", "…_old.json") that reuse a live profile's name but are not in the index, so a raw
        /// directory scan is only the fallback for a vendor without an index — and even then one profile per name.
        /// </summary>
        protected List<SystemProfile> FindSelectableSystemProfiles()
        {
            var result = new List<SystemProfile>();
            var systemDir = Path.Combine(GetConfigRoot(), "system");
            if (!Directory.Exists(systemDir)) return result;

            foreach (var vendorDir in Directory.EnumerateDirectories(systemDir).Order(StringComparer.Ordinal))
            {
                var seenNames = new HashSet<string>(StringComparer.Ordinal);
                foreach (var file in EnumerateVendorProcessFiles(vendorDir))
                {
                    var profile = ReadSelectableProfile(file);
                    if (profile != null && seenNames.Add(profile.Name))
                        result.Add(profile);
                }
            }

            return result;
        }

        private static IEnumerable<string> EnumerateVendorProcessFiles(string vendorDir)
        {
            var indexPath = vendorDir + ".json";
            if (File.Exists(indexPath))
            {
                JsonNode? index;
                try { index = JsonNode.Parse(File.ReadAllText(indexPath)); }
                catch (JsonException) { index = null; }

                var processList = index?["process_list"]?.AsArray();
                if (processList != null)
                {
                    return processList
                        .Select(entry => entry?["sub_path"]?.GetValue<string>())
                        .Where(subPath => !string.IsNullOrEmpty(subPath))
                        .Select(subPath => Path.Combine(vendorDir, subPath!))
                        .Where(File.Exists)
                        .ToList();
                }
            }

            var processDir = Path.Combine(vendorDir, "process");
            return Directory.Exists(processDir)
                ? Directory.EnumerateFiles(processDir, "*.json", SearchOption.AllDirectories).Order(StringComparer.Ordinal)
                : [];
        }

        private static SystemProfile? ReadSelectableProfile(string file)
        {
            // JsonDocument rather than JsonNode: vendors ship the odd profile with a duplicated key, which JsonNode
            // rejects outright while the slicer (and JsonDocument) simply take the last value.
            try
            {
                using var document = JsonDocument.Parse(File.ReadAllText(file));
                var root = document.RootElement;
                if (root.ValueKind != JsonValueKind.Object) return null;

                if (GetString(root, "instantiation") != "true") return null;

                var name = GetString(root, "name");
                if (string.IsNullOrEmpty(name)) return null;

                return new SystemProfile(name, file, GetString(root, "version") ?? "");
            }
            catch (JsonException)
            {
                return null;
            }
        }

        private static string? GetString(JsonElement element, string property) =>
            element.TryGetProperty(property, out var value) && value.ValueKind == JsonValueKind.String
                ? value.GetString()
                : null;

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
