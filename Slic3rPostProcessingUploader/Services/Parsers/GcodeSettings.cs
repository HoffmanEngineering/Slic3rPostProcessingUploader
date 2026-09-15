namespace Slic3rPostProcessingUploader.Services.Parsers
{
    /// <summary>
    /// A single-pass index of the "; key = value" comment lines in a gcode file, so that each setting lookup is a
    /// dictionary hit instead of a scan of the whole file.
    /// </summary>
    internal sealed class GcodeSettings
    {
        private readonly Dictionary<string, string> values;

        private GcodeSettings(Dictionary<string, string> values)
        {
            this.values = values;
        }

        /// <summary>
        /// Parses every line of the form "; key SEP value" where SEP is one of <paramref name="separators"/> surrounded by
        /// single spaces. When a key repeats, the first non-empty value wins.
        /// </summary>
        public static GcodeSettings Parse(string gcode, ReadOnlySpan<char> separators)
        {
            var values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            foreach (var rawLine in gcode.AsSpan().EnumerateLines())
            {
                var line = rawLine.TrimEnd('\r');
                if (!line.StartsWith("; "))
                {
                    continue;
                }

                line = line.Slice(2);

                int separatorIndex = FindSeparator(line, separators);
                if (separatorIndex < 0)
                {
                    continue;
                }

                var key = line.Slice(0, separatorIndex);
                var value = line.Slice(separatorIndex + 3).Trim();
                if (value.IsEmpty)
                {
                    continue;
                }

                values.TryAdd(key.ToString(), value.ToString());
            }

            return new GcodeSettings(values);
        }

        /// <summary>
        /// Index of the first " SEP " in the line, or -1.
        /// </summary>
        private static int FindSeparator(ReadOnlySpan<char> line, ReadOnlySpan<char> separators)
        {
            int index = 1;
            while (index < line.Length - 1)
            {
                int found = line.Slice(index, line.Length - index - 1).IndexOfAny(separators);
                if (found < 0)
                {
                    return -1;
                }

                found += index;
                if (line[found - 1] == ' ' && line[found + 1] == ' ')
                {
                    return found - 1;
                }

                index = found + 1;
            }

            return -1;
        }

        /// <summary>
        /// Returns the value for the key, or an empty string when the key is not present.
        /// </summary>
        public string Get(string key)
        {
            return values.TryGetValue(key, out var value) ? value : string.Empty;
        }

        /// <summary>
        /// Stores a computed value under the key, replacing any "; key = value" line of the same name.
        /// </summary>
        public void Set(string key, string value)
        {
            values[key] = value;
        }
    }
}
