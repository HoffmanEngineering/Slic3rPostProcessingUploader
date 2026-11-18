using System.Text.Json;

namespace PrintLog.SlicerSettings.Computed
{
    /// <summary>
    /// One preset the G-code was sliced with. <paramref name="Type"/> is the folder under <c>user/&lt;account&gt;/</c>
    /// (process, filament, machine). A system preset (empty <c>inherits_group</c> entry) has no user file and nothing
    /// saved in it; a user preset must have exactly one file or the locator gives up.
    /// </summary>
    internal sealed record PresetIdentity(string Type, string Name, bool IsUserPreset);

    /// <summary>
    /// Every setting value found in the located user preset files, keyed by setting. A key can legitimately appear in
    /// more than one preset (e.g. a filament override and a process override), so all values are kept.
    /// </summary>
    internal sealed class SavedPresetValues
    {
        private readonly Dictionary<string, List<string[]>> values = new(StringComparer.Ordinal);

        internal void Add(string key, string[] elements)
        {
            if (!values.TryGetValue(key, out var list))
            {
                values[key] = list = [];
            }

            list.Add(elements.Select(e => e.Trim()).ToArray());
        }

        /// <summary>
        /// True when some located preset file saves this key with this value. Orca writes list settings into the
        /// G-code with ',' for numeric and point vectors (nozzle_temperature = 215,215; printable_area = 0x0,250x0,…)
        /// and ';' for string vectors (filament_type = PLA;PETG), so a JSON array matches on either join.
        /// </summary>
        public bool IsSaved(string key, string gcodeValue)
        {
            if (!values.TryGetValue(key, out var candidates))
            {
                return false;
            }

            var wanted = gcodeValue.Trim();
            return candidates.Any(elements =>
                string.Join(",", elements) == wanted || string.Join(";", elements) == wanted);
        }
    }

    /// <summary>
    /// Finds the user's own preset files under one or more Orca-family config roots
    /// (<c>&lt;root&gt;/user/&lt;account&gt;/&lt;type&gt;/&lt;name&gt;.json</c>). These files hold only the settings the user
    /// saved over the system parent, which is what separates a saved change from an unsaved plater edit.
    /// <see cref="FromFiles"/> does the same lookup over preset files a client read on the slicing machine.
    /// </summary>
    internal sealed class UserPresetLocator
    {
        /// <summary>
        /// One file that may hold a preset. <paramref name="Key"/> identifies the file, so a preset the G-code names
        /// more than once (the same filament in every slot) is read once.
        /// </summary>
        private sealed record Candidate(object Key, string Location, Func<JsonDocument> Parse);

        private readonly Action<string> debugLog;

        /// <summary>Called once per <see cref="FindSavedValues"/>; returns the lookup from a preset to its files.</summary>
        private readonly Func<Func<PresetIdentity, IReadOnlyList<Candidate>>> prepare;

        public UserPresetLocator(IEnumerable<string> configRoots, Action<string> debugLog)
        {
            this.debugLog = debugLog;
            prepare = () =>
            {
                var accountDirs = FindAccountDirs(configRoots, debugLog);
                return preset => accountDirs
                    .Select(accountDir => Path.Combine(accountDir, preset.Type, preset.Name + ".json"))
                    .Where(File.Exists)
                    .Select(path => new Candidate(path, path, () =>
                    {
                        using var stream = File.OpenRead(path);
                        return JsonDocument.Parse(stream);
                    }))
                    .ToList();
            };
        }

        private UserPresetLocator(Action<string> debugLog, Func<Func<PresetIdentity, IReadOnlyList<Candidate>>> prepare)
        {
            this.debugLog = debugLog;
            this.prepare = prepare;
        }

        /// <summary>
        /// A locator over preset files that are already in memory. A file matches a preset when its kind is the
        /// preset's type folder (<c>process</c>, <c>filament</c>, <c>machine</c>) and its name is the preset name,
        /// both compared ordinally: the same rule as one file per account folder on disk, so two files with the same
        /// kind and name are as ambiguous as the same preset under two accounts. Null entries are ignored.
        /// </summary>
        public static UserPresetLocator FromFiles(IEnumerable<UserPresetFile?> files, Action<string> debugLog) =>
            new(debugLog, () =>
            {
                var byIdentity = new Dictionary<(string Kind, string Name), List<Candidate>>();
                int index = 0;
                foreach (var file in files)
                {
                    index++;
                    if (file == null)
                    {
                        continue;
                    }

                    var key = (file.Kind ?? string.Empty, file.Name ?? string.Empty);
                    if (!byIdentity.TryGetValue(key, out var list))
                    {
                        byIdentity[key] = list = [];
                    }

                    var json = file.Json;
                    list.Add(new Candidate(file, $"supplied file {index}", () => JsonDocument.Parse(json)));
                }

                return preset => byIdentity.TryGetValue((preset.Type, preset.Name), out var list) ? list : [];
            });

        /// <summary>
        /// Returns the saved values across all identities, or null when any user preset cannot be pinned to exactly
        /// one readable file — the caller then falls back to the plain "changed" list rather than guessing.
        /// </summary>
        public SavedPresetValues? FindSavedValues(IEnumerable<PresetIdentity> presets)
        {
            var find = prepare();
            var saved = new SavedPresetValues();
            var read = new HashSet<object>();

            foreach (var preset in presets.Where(p => p.IsUserPreset))
            {
                if (!IsSafePresetName(preset.Name))
                {
                    debugLog($"{preset.Type} preset name \"{preset.Name}\" is not a plain file name; cannot tell saved from unsaved changes");
                    return null;
                }

                var candidates = find(preset);
                if (candidates.Count != 1)
                {
                    debugLog($"{preset.Type} preset \"{preset.Name}\": {candidates.Count} files found, cannot tell saved from unsaved changes");
                    return null;
                }

                var candidate = candidates[0];
                if (!read.Add(candidate.Key))
                {
                    continue;
                }

                try
                {
                    using var document = candidate.Parse();
                    ReadValues(document, saved);
                }
                catch (Exception e) when (e is IOException or UnauthorizedAccessException or JsonException or ArgumentException)
                {
                    debugLog($"{preset.Type} preset \"{preset.Name}\": could not read {candidate.Location}: {e.Message}");
                    return null;
                }
            }

            return saved;
        }

        private static List<string> FindAccountDirs(IEnumerable<string> configRoots, Action<string> debugLog)
        {
            var dirs = new List<string>();
            foreach (var root in configRoots)
            {
                var userDir = Path.Combine(root, "user");
                if (!Directory.Exists(userDir))
                {
                    continue;
                }

                try
                {
                    dirs.AddRange(Directory.GetDirectories(userDir));
                }
                catch (Exception e) when (e is IOException or UnauthorizedAccessException)
                {
                    debugLog($"Skipping {userDir}: {e.Message}");
                }
            }

            return dirs;
        }

        /// <summary>
        /// The preset name comes from the G-code, so it is treated as untrusted: only a plain file name (no separators
        /// of either platform, no drive letter, no traversal, no characters the host rejects) may be joined onto the
        /// preset-type folder. Anything else is reported as not found. The user's own config directory is the trust
        /// boundary; links planted inside it are not defended against.
        /// </summary>
        internal static bool IsSafePresetName(string name) =>
            name.Length > 0
            && name != "." && name != ".."
            && name.IndexOfAny(['/', '\\', ':']) < 0
            && name.IndexOfAny(Path.GetInvalidFileNameChars()) < 0;

        /// <summary>
        /// Reads the flat key/value pairs of a preset file. Real preset files can contain duplicate keys, which
        /// <see cref="JsonDocument"/> tolerates (the first occurrence wins here). Nested objects are not settings and
        /// are skipped.
        /// </summary>
        private static void ReadValues(JsonDocument document, SavedPresetValues saved)
        {
            if (document.RootElement.ValueKind != JsonValueKind.Object)
            {
                throw new JsonException("preset root is not an object");
            }

            var seen = new HashSet<string>(StringComparer.Ordinal);
            foreach (var property in document.RootElement.EnumerateObject())
            {
                if (!seen.Add(property.Name))
                {
                    continue;
                }

                var elements = Elements(property.Value);
                if (elements != null)
                {
                    saved.Add(property.Name, elements);
                }
            }
        }

        private static string[]? Elements(JsonElement element) => element.ValueKind switch
        {
            JsonValueKind.Array => element.EnumerateArray().Select(Scalar).OfType<string>().ToArray(),
            _ => Scalar(element) is { } scalar ? [scalar] : null,
        };

        private static string? Scalar(JsonElement element) => element.ValueKind switch
        {
            JsonValueKind.String => element.GetString(),
            JsonValueKind.Number or JsonValueKind.True or JsonValueKind.False => element.GetRawText(),
            _ => null,
        };
    }
}
