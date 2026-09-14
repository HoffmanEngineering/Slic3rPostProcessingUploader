namespace Slic3rPostProcessingUploader.Services.Parsers.Computed
{
    /// <summary>
    /// {{modified_settings}}: which settings differ from the system profile the print was based on. Orca lists the
    /// keys in <c>different_settings_to_system</c> (live preset vs. system parent, so unsaved plater edits count);
    /// each key's value is taken from the config block so the note reads "sparse_infill_density = 8%".
    /// </summary>
    internal static class ModifiedSettingsPlaceholder
    {
        public static readonly ComputedPlaceholder Instance = new("modified_settings", Render);

        private static string Render(ComputedContext context)
        {
            var settings = context.Settings;
            var keys = ModifiedSettingsParser.ParseKeys(settings.Get("different_settings_to_system"));
            if (keys.Count == 0)
            {
                return string.Empty;
            }

            var parents = ModifiedSettingsParser.ParseEntries(settings.Get("inherits_group"));
            var processParent = parents.Count > 0 ? parents[0] : string.Empty;
            var heading = processParent.Length > 0 ? $"Changed from \"{processParent}\":" : "Changed from profile:";

            var changes = keys.Select(key => Describe(key, settings.Get(key)));
            return $"{heading} {string.Join(", ", changes)}";
        }

        private static string Describe(string key, string value) =>
            value.Length > 0 ? $"{key} = {value}" : $"{key} = (not in G-code)";
    }
}
