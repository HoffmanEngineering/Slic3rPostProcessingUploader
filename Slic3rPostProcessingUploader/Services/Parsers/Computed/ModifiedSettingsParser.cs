using System.Text;

namespace Slic3rPostProcessingUploader.Services.Parsers.Computed
{
    /// <summary>
    /// Reads Orca's ';'-separated list settings. <c>inherits_group</c> and <c>different_settings_to_system</c> are
    /// positional (process; one entry per filament; printer) and use C-style quoting for entries that contain spaces
    /// or semicolons, mirroring <c>escape_strings_cstyle</c> in libslic3r.
    /// </summary>
    internal static class ModifiedSettingsParser
    {
        /// <summary>
        /// The uploader's own installer adds this key to every profile it manages, so it is never a user change.
        /// </summary>
        private const string PostProcessKey = "post_process";

        /// <summary>
        /// Splits the value into its positional entries, unquoting and trimming each. Empty entries are kept because the
        /// position identifies which preset (process, filament n, printer) the entry belongs to.
        /// </summary>
        public static IReadOnlyList<string> ParseEntries(string value)
        {
            var entries = new List<string>();
            if (value.Length == 0)
            {
                return entries;
            }

            var current = new StringBuilder();
            bool quoted = false;
            for (int i = 0; i < value.Length; i++)
            {
                char c = value[i];
                if (quoted)
                {
                    if (c == '\\' && i + 1 < value.Length)
                    {
                        current.Append(value[++i]);
                    }
                    else if (c == '"')
                    {
                        quoted = false;
                    }
                    else
                    {
                        current.Append(c);
                    }
                }
                else if (c == '"')
                {
                    quoted = true;
                }
                else if (c == ';')
                {
                    entries.Add(current.ToString().Trim());
                    current.Clear();
                }
                else
                {
                    current.Append(c);
                }
            }

            entries.Add(current.ToString().Trim());
            return entries;
        }

        /// <summary>
        /// The distinct setting keys in a <c>different_settings_to_system</c> value, in first-seen order, without the
        /// uploader's own <c>post_process</c>. Older Orca builds do not quote the per-preset inner lists, so entry
        /// boundaries are not trusted: every entry is split on ';' again and the result is treated as one flat set.
        /// </summary>
        public static IReadOnlyList<string> ParseKeys(string value)
        {
            var seen = new HashSet<string>(StringComparer.Ordinal);
            var keys = new List<string>();
            foreach (var entry in ParseEntries(value))
            {
                foreach (var rawKey in entry.Split(';'))
                {
                    var key = rawKey.Trim();
                    if (key.Length > 0 && key != PostProcessKey && seen.Add(key))
                    {
                        keys.Add(key);
                    }
                }
            }

            return keys;
        }
    }
}
