using Slic3rPostProcessingUploader.Services.Installer;

namespace Slic3rPostProcessingUploader.Services.Parsers.Computed
{
    /// <summary>
    /// {{modified_settings}}: which settings differ from the system profile the print was based on. Orca lists the
    /// keys in <c>different_settings_to_system</c> (live preset vs. system parent, so unsaved plater edits count);
    /// each key's value is taken from the config block so the note reads "sparse_infill_density = 8%".
    ///
    /// When the user's own preset files can be found on this machine, the list is split into changes saved in a
    /// preset and changes that were only made in the plater. When they cannot (other machine, ambiguous names,
    /// unreadable files) the flat list is shown instead — never a wrong classification.
    /// </summary>
    internal static class ModifiedSettingsPlaceholder
    {
        public static readonly ComputedPlaceholder Instance = WithConfigRoots(SlicerInstallerRegistry.OrcaFamilyConfigRoots);

        /// <summary>Builds the placeholder against specific config roots; the roots are only resolved when needed.</summary>
        public static ComputedPlaceholder WithConfigRoots(Func<IEnumerable<string>> configRoots) =>
            new("modified_settings", context => Render(context, configRoots));

        private static string Render(ComputedContext context, Func<IEnumerable<string>> configRoots)
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

            var changes = keys.Select(key => (Key: key, Text: Describe(key, settings.Get(key)))).ToList();

            // Without inherits_group there is no way to know which presets are the user's own, so no claim is made.
            var saved = parents.Count == 0 ? null : FindSavedValues(context, configRoots, settings, parents);
            if (saved == null)
            {
                return $"{heading} {string.Join(", ", changes.Select(c => c.Text))}";
            }

            var savedChanges = changes.Where(c => saved.IsSaved(c.Key, settings.Get(c.Key))).Select(c => c.Text).ToList();
            var unsavedChanges = changes.Where(c => !saved.IsSaved(c.Key, settings.Get(c.Key))).Select(c => c.Text).ToList();

            var lines = new List<string> { heading };
            if (savedChanges.Count > 0)
            {
                lines.Add($"  Saved in profile: {string.Join(", ", savedChanges)}");
            }

            if (unsavedChanges.Count > 0)
            {
                lines.Add($"  Unsaved changes:  {string.Join(", ", unsavedChanges)}");
            }

            return string.Join("\n", lines);
        }

        private static SavedPresetValues? FindSavedValues(
            ComputedContext context, Func<IEnumerable<string>> configRoots, GcodeSettings settings, IReadOnlyList<string> parents)
        {
            try
            {
                var locator = new UserPresetLocator(configRoots(), context.DebugLog);
                return locator.FindSavedValues(PresetIdentities(settings, parents));
            }
            catch (Exception e)
            {
                context.DebugLog($"Could not check which changed settings are saved in a preset: {e.Message}");
                return null;
            }
        }

        /// <summary>
        /// The presets the file was sliced with, in the same order as <c>inherits_group</c>: process, one per
        /// filament, printer. A preset whose inherits entry is empty is a system preset (or a custom one with no
        /// parent) and has no user file to consult.
        /// </summary>
        private static IEnumerable<PresetIdentity> PresetIdentities(GcodeSettings settings, IReadOnlyList<string> parents)
        {
            var filaments = ModifiedSettingsParser.ParseEntries(settings.Get("filament_settings_id"));

            yield return Identity("process", settings.Get("print_settings_id"), parents, 0);
            for (int i = 0; i < filaments.Count; i++)
            {
                yield return Identity("filament", filaments[i], parents, i + 1);
            }

            yield return Identity("machine", settings.Get("printer_settings_id"), parents, filaments.Count + 1);
        }

        private static PresetIdentity Identity(string type, string name, IReadOnlyList<string> parents, int position) =>
            new(type, name, IsUserPreset: position < parents.Count && parents[position].Length > 0);

        private static string Describe(string key, string value) =>
            value.Length > 0 ? $"{key} = {value}" : $"{key} = (not in G-code)";
    }
}
