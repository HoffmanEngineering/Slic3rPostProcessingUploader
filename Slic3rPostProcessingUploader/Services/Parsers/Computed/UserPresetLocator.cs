using System.Text.Json;

namespace Slic3rPostProcessingUploader.Services.Parsers.Computed
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
        private readonly Dictionary<string, HashSet<string>> values = new(StringComparer.Ordinal);

        internal void Add(string key, string value)
        {
            if (!values.TryGetValue(key, out var set))
            {
                values[key] = set = new HashSet<string>(StringComparer.Ordinal);
            }

            set.Add(value.Trim());
        }

        /// <summary>True when some located preset file saves this key with this value.</summary>
        public bool IsSaved(string key, string gcodeValue) =>
            values.TryGetValue(key, out var set) && set.Contains(gcodeValue.Trim());
    }

    /// <summary>
    /// Finds the user's own preset files under one or more Orca-family config roots
    /// (<c>&lt;root&gt;/user/&lt;account&gt;/&lt;type&gt;/&lt;name&gt;.json</c>). These files hold only the settings the user
    /// saved over the system parent, which is what separates a saved change from an unsaved plater edit.
    /// </summary>
    internal sealed class UserPresetLocator(IEnumerable<string> configRoots, Action<string> debugLog)
    {
        /// <summary>
        /// Returns the saved values across all identities, or null when any user preset cannot be pinned to exactly
        /// one readable file — the caller then falls back to the plain "changed" list rather than guessing.
        /// </summary>
        public SavedPresetValues? FindSavedValues(IEnumerable<PresetIdentity> presets)
        {
            var accountDirs = FindAccountDirs();
            var saved = new SavedPresetValues();

            foreach (var preset in presets.Where(p => p.IsUserPreset))
            {
                var candidates = accountDirs
                    .Select(accountDir => SafePresetPath(accountDir, preset))
                    .Where(path => path != null && File.Exists(path))
                    .ToList();

                if (candidates.Count != 1)
                {
                    debugLog($"{preset.Type} preset \"{preset.Name}\": {candidates.Count} files found, cannot tell saved from unsaved changes");
                    return null;
                }

                try
                {
                    ReadValues(candidates[0]!, saved);
                }
                catch (Exception e) when (e is IOException or UnauthorizedAccessException or JsonException)
                {
                    debugLog($"{preset.Type} preset \"{preset.Name}\": could not read {candidates[0]}: {e.Message}");
                    return null;
                }
            }

            return saved;
        }

        private List<string> FindAccountDirs()
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
        /// The preset name comes from the G-code, so it is treated as untrusted: it must be a plain file name and the
        /// resulting path must stay inside the preset-type folder.
        /// </summary>
        private static string? SafePresetPath(string accountDir, PresetIdentity preset)
        {
            var name = preset.Name;
            if (name.Length == 0
                || name != Path.GetFileName(name)
                || name.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0
                || name == "." || name == "..")
            {
                return null;
            }

            var typeDir = Path.GetFullPath(Path.Combine(accountDir, preset.Type));
            var path = Path.GetFullPath(Path.Combine(typeDir, name + ".json"));
            return path.StartsWith(typeDir + Path.DirectorySeparatorChar, StringComparison.Ordinal) ? path : null;
        }

        /// <summary>
        /// Reads the flat key/value pairs of a preset file. Real preset files can contain duplicate keys, which
        /// <see cref="JsonDocument"/> tolerates (the first occurrence wins here). Lists are joined with ';' to match how
        /// Orca serialises them into the G-code config block; nested objects are not settings and are skipped.
        /// </summary>
        private static void ReadValues(string path, SavedPresetValues saved)
        {
            using var stream = File.OpenRead(path);
            using var document = JsonDocument.Parse(stream);
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

                var value = Normalise(property.Value);
                if (value != null)
                {
                    saved.Add(property.Name, value);
                }
            }
        }

        private static string? Normalise(JsonElement element) => element.ValueKind switch
        {
            JsonValueKind.String => element.GetString(),
            JsonValueKind.Number or JsonValueKind.True or JsonValueKind.False => element.GetRawText(),
            JsonValueKind.Array => string.Join(";", element.EnumerateArray().Select(Normalise).Where(v => v != null)),
            _ => null,
        };
    }
}
