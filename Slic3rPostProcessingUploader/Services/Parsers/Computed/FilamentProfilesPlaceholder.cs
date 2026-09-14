namespace Slic3rPostProcessingUploader.Services.Parsers.Computed
{
    /// <summary>
    /// {{filament_profiles}}: the filament profiles the print used, one entry per distinct profile with a count.
    /// Orca repeats the profile for every slot ("A";"A";"A";"A";"A" on a five-slot printer), which is noise to read.
    /// Renders e.g. <c>Snapmaker PLA @U1 (PLA, Snapmaker) ×5</c> or <c>A (PLA, X) ×2, B (PETG, Y)</c>.
    /// Colours are left out: the per-slot filament usage already carries them.
    /// </summary>
    internal static class FilamentProfilesPlaceholder
    {
        public static readonly ComputedPlaceholder Instance = new("filament_profiles", Render);

        private sealed record Profile(string Name, string Type, string Vendor);

        private static string Render(ComputedContext context)
        {
            var settings = context.Settings;
            var names = ModifiedSettingsParser.ParseEntries(settings.Get("filament_settings_id"));
            var types = ModifiedSettingsParser.ParseEntries(settings.Get("filament_type"));
            var vendors = ModifiedSettingsParser.ParseEntries(settings.Get("filament_vendor"));

            var counts = new List<(Profile Profile, int Count)>();
            for (int i = 0; i < names.Count; i++)
            {
                if (names[i].Length == 0)
                {
                    continue;
                }

                var profile = new Profile(names[i], types.ElementAtOrDefault(i) ?? string.Empty, vendors.ElementAtOrDefault(i) ?? string.Empty);
                int index = counts.FindIndex(c => c.Profile == profile);
                if (index < 0)
                {
                    counts.Add((profile, 1));
                }
                else
                {
                    counts[index] = (profile, counts[index].Count + 1);
                }
            }

            return string.Join(", ", counts.Select(c => Describe(c.Profile) + (c.Count > 1 ? $" ×{c.Count}" : string.Empty)));
        }

        private static string Describe(Profile profile)
        {
            var details = new[] { profile.Type, profile.Vendor }.Where(d => d.Length > 0).ToList();
            return details.Count > 0 ? $"{profile.Name} ({string.Join(", ", details)})" : profile.Name;
        }
    }
}
