namespace Slic3rPostProcessingUploader.Services.Parsers.Computed
{
    /// <summary>
    /// Finds the user's own PrusaSlicer preset files (<c>&lt;root&gt;/{print,filament,printer}/&lt;name&gt;.ini</c>) under one
    /// or more config roots. Unlike Orca's, a PrusaSlicer user preset file is a full dump of the preset, so its values
    /// are what the slicer would have written had nothing been changed in the plater.
    /// </summary>
    internal sealed class PrusaPresetLocator(IEnumerable<string> configRoots, Action<string> debugLog)
    {
        /// <summary>
        /// The flat key/value pairs of the one user preset file for <paramref name="name"/>, or null when it cannot be
        /// pinned to exactly one readable file — the caller then says nothing about that preset rather than guess.
        /// A system preset has no user file, so it lands here too.
        /// </summary>
        public IReadOnlyDictionary<string, string>? Read(string type, string name)
        {
            if (!UserPresetLocator.IsSafePresetName(name))
            {
                debugLog($"{type} preset name \"{name}\" is not a plain file name; cannot check for unsaved changes");
                return null;
            }

            var candidates = configRoots
                .Select(root => Path.Combine(root, type, name + ".ini"))
                .Where(File.Exists)
                .ToList();

            if (candidates.Count != 1)
            {
                debugLog($"{type} preset \"{name}\": {candidates.Count} files found, cannot check for unsaved changes");
                return null;
            }

            try
            {
                return Parse(File.ReadLines(candidates[0]));
            }
            catch (Exception e) when (e is IOException or UnauthorizedAccessException)
            {
                debugLog($"{type} preset \"{name}\": could not read {candidates[0]}: {e.Message}");
                return null;
            }
        }

        /// <summary>
        /// A preset file is "key = value" lines with '#' comments and no sections; the first occurrence of a key wins.
        /// </summary>
        internal static IReadOnlyDictionary<string, string> Parse(IEnumerable<string> lines)
        {
            var values = new Dictionary<string, string>(StringComparer.Ordinal);
            foreach (var line in lines)
            {
                if (line.Length == 0 || line[0] == '#')
                {
                    continue;
                }

                int separator = line.IndexOf('=');
                if (separator <= 0)
                {
                    continue;
                }

                values.TryAdd(line[..separator].Trim(), line[(separator + 1)..].Trim());
            }

            return values;
        }
    }
}
