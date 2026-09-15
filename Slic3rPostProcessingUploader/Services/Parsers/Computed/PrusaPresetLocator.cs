namespace Slic3rPostProcessingUploader.Services.Parsers.Computed
{
    /// <summary>
    /// Finds the presets a PrusaSlicer print was sliced with under one or more config roots and returns each as the
    /// flat key/value dump the slicer would have written had nothing been changed in the plater:
    /// <list type="bullet">
    /// <item>a user preset is <c>&lt;root&gt;/{print,filament,printer}/&lt;name&gt;.ini</c>, a full dump of the preset;</item>
    /// <item>a system preset is a <c>[type:name]</c> section of a vendor bundle, <c>&lt;root&gt;/vendor/*.ini</c>, whose
    /// values are resolved through its <c>inherits</c> chain (later parents override earlier ones, the section's own
    /// keys override them all).</item>
    /// </list>
    /// The user file wins when both exist, because that is what the slicer loaded.
    /// </summary>
    internal sealed class PrusaPresetLocator(IEnumerable<string> configRoots, Action<string> debugLog)
    {
        // A multi-slot printer names the same filament preset once per slot; look it up (and log about it) once.
        private readonly Dictionary<(string Type, string Name), IReadOnlyDictionary<string, string>?> cache = [];

        // Vendor bundles are a few MB each; parse each one once per run, and only when a system preset is asked for.
        private Dictionary<string, Dictionary<string, Dictionary<string, string>>>? bundles;

        /// <summary>
        /// The flat key/value pairs of the preset, or null when it cannot be pinned to exactly one readable source —
        /// the caller then says nothing about that preset rather than guess.
        /// </summary>
        public IReadOnlyDictionary<string, string>? Read(string type, string name)
        {
            if (!cache.TryGetValue((type, name), out var values))
            {
                cache[(type, name)] = values = Locate(type, name);
            }

            return values;
        }

        private IReadOnlyDictionary<string, string>? Locate(string type, string name)
        {
            if (!UserPresetLocator.IsSafePresetName(name))
            {
                debugLog($"{type} preset name \"{name}\" is not a plain file name; cannot check for unsaved changes");
                return null;
            }

            var userFiles = configRoots
                .Select(root => Path.Combine(root, type, name + ".ini"))
                .Where(File.Exists)
                .ToList();

            if (userFiles.Count > 1)
            {
                debugLog($"{type} preset \"{name}\": {userFiles.Count} user preset files found, cannot check for unsaved changes");
                return null;
            }

            if (userFiles.Count == 1)
            {
                try
                {
                    return Parse(File.ReadLines(userFiles[0]));
                }
                catch (Exception e) when (e is IOException or UnauthorizedAccessException)
                {
                    debugLog($"{type} preset \"{name}\": could not read {userFiles[0]}: {e.Message}");
                    return null;
                }
            }

            return ResolveSystemPreset(type, name);
        }

        /// <summary>
        /// Looks the section up in every vendor bundle; it must exist in exactly one, so that "Generic PLA" from two
        /// vendors is never confused. Presets are resolved within the bundle that defines them.
        /// </summary>
        private IReadOnlyDictionary<string, string>? ResolveSystemPreset(string type, string name)
        {
            var section = $"{type}:{name}";
            var owners = Bundles().Where(b => b.Value.ContainsKey(section)).ToList();
            if (owners.Count != 1)
            {
                debugLog($"{type} preset \"{name}\": no user preset file and {owners.Count} vendor bundles define it, cannot check for unsaved changes");
                return null;
            }

            var resolved = new Dictionary<string, string>(StringComparer.Ordinal);
            if (!Resolve(owners[0].Value, type, name, resolved, []))
            {
                debugLog($"{type} preset \"{name}\": its inherits chain in {owners[0].Key} is broken, cannot check for unsaved changes");
                return null;
            }

            return resolved;
        }

        private static bool Resolve(Dictionary<string, Dictionary<string, string>> bundle, string type, string name, Dictionary<string, string> into, HashSet<string> visiting)
        {
            var section = $"{type}:{name}";
            if (!bundle.TryGetValue(section, out var values) || !visiting.Add(section))
            {
                return false;
            }

            if (values.TryGetValue("inherits", out var inherits))
            {
                foreach (var parent in inherits.Split(';').Select(p => p.Trim()).Where(p => p.Length > 0))
                {
                    if (!Resolve(bundle, type, parent, into, visiting))
                    {
                        return false;
                    }
                }
            }

            foreach (var (key, value) in values)
            {
                if (key != "inherits")
                {
                    into[key] = value;
                }
            }

            visiting.Remove(section);
            return true;
        }

        private Dictionary<string, Dictionary<string, Dictionary<string, string>>> Bundles()
        {
            if (bundles != null)
            {
                return bundles;
            }

            bundles = new Dictionary<string, Dictionary<string, Dictionary<string, string>>>(StringComparer.Ordinal);
            foreach (var root in configRoots)
            {
                var vendorDir = Path.Combine(root, "vendor");
                if (!Directory.Exists(vendorDir))
                {
                    continue;
                }

                IEnumerable<string> files;
                try
                {
                    files = Directory.EnumerateFiles(vendorDir, "*.ini");
                }
                catch (Exception e) when (e is IOException or UnauthorizedAccessException)
                {
                    debugLog($"Skipping {vendorDir}: {e.Message}");
                    continue;
                }

                foreach (var file in files)
                {
                    try
                    {
                        bundles[file] = ParseBundle(File.ReadLines(file));
                    }
                    catch (Exception e) when (e is IOException or UnauthorizedAccessException or FormatException)
                    {
                        debugLog($"Skipping vendor bundle {file}: {e.Message}");
                    }
                }
            }

            return bundles;
        }

        /// <summary>
        /// A preset file is "key = value" lines with '#' comments and no sections; the first occurrence of a key wins.
        /// </summary>
        internal static IReadOnlyDictionary<string, string> Parse(IEnumerable<string> lines)
        {
            var values = new Dictionary<string, string>(StringComparer.Ordinal);
            foreach (var line in lines)
            {
                if (TryParseKeyValue(line, out var key, out var value))
                {
                    values.TryAdd(key, value);
                }
            }

            return values;
        }

        /// <summary>
        /// A vendor bundle is the same lines grouped under "[type:name]" headers. Lines before the first header
        /// (config_version and friends) are not presets and are dropped.
        /// </summary>
        internal static Dictionary<string, Dictionary<string, string>> ParseBundle(IEnumerable<string> lines)
        {
            var sections = new Dictionary<string, Dictionary<string, string>>(StringComparer.Ordinal);
            Dictionary<string, string>? current = null;
            foreach (var line in lines)
            {
                if (line.StartsWith('['))
                {
                    if (!line.EndsWith(']'))
                    {
                        throw new FormatException($"unterminated section header: {line}");
                    }

                    var header = line[1..^1].Trim();
                    if (!sections.TryGetValue(header, out current))
                    {
                        sections[header] = current = new Dictionary<string, string>(StringComparer.Ordinal);
                    }
                }
                else if (current != null && TryParseKeyValue(line, out var key, out var value))
                {
                    current.TryAdd(key, value);
                }
            }

            return sections;
        }

        private static bool TryParseKeyValue(string line, out string key, out string value)
        {
            key = value = string.Empty;
            if (line.Length == 0 || line[0] == '#')
            {
                return false;
            }

            int separator = line.IndexOf('=');
            if (separator <= 0)
            {
                return false;
            }

            key = line[..separator].Trim();
            value = line[(separator + 1)..].Trim();
            return key.Length > 0;
        }
    }
}
