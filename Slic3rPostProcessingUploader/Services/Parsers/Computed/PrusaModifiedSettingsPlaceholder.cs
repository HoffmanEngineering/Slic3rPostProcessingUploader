namespace Slic3rPostProcessingUploader.Services.Parsers.Computed
{
    /// <summary>
    /// {{modified_settings}} for PrusaSlicer: the settings that were changed in the plater and never saved. PrusaSlicer
    /// G-code carries no list of changed keys, but the preset it was sliced with can be reconstructed on the same
    /// machine — a user preset from its .ini file (a full dump), a system preset from the vendor bundle — so every key
    /// whose G-code value differs from the preset's is an unsaved change: the kind a reader cannot recover by
    /// selecting the same presets again.
    ///
    /// Each preset type is checked on its own: the print preset, every filament slot (a per-filament key is compared
    /// against the slots' presets joined up), and the printer preset. A preset that cannot be found (another machine,
    /// an ambiguous name, an unreadable file) is skipped with a debug note rather than guessed at, so the section can
    /// only ever under-report.
    /// </summary>
    internal static class PrusaModifiedSettingsPlaceholder
    {
        public static readonly ComputedPlaceholder Instance = new("modified_settings", Render);

        /// <summary>
        /// Keys that describe the preset rather than the print: the ids are renamed on save, the uploader's installer
        /// writes post_process itself, and the compatibility conditions, aliases and rename history of a vendor
        /// preset never reach the G-code.
        /// </summary>
        private static readonly HashSet<string> IgnoredKeys = new(StringComparer.Ordinal)
        {
            "inherits", "post_process", "alias", "renamed_from",
            "print_settings_id", "filament_settings_id", "printer_settings_id", "physical_printer_settings_id",
            "compatible_printers", "compatible_printers_condition", "compatible_prints", "compatible_prints_condition",
        };

        private static string Render(ComputedContext context)
        {
            var settings = context.Settings;
            var changes = new List<string>();

            try
            {
                var locator = new PrusaPresetLocator(context.ConfigRoots(), context.DebugLog);
                Compare(settings, [locator.Read("print", settings.Get("print_settings_id"))], changes);
                Compare(settings, FilamentPresets(settings, locator), changes);
                Compare(settings, [locator.Read("printer", settings.Get("printer_settings_id"))], changes);
            }
            catch (Exception e)
            {
                context.DebugLog($"Could not check the presets for unsaved changes: {e.Message}");
                return string.Empty;
            }

            return changes.Count == 0 ? string.Empty : $"Profile Changes:\n  Unsaved changes: {string.Join(", ", changes)}\n";
        }

        /// <summary>
        /// One preset per slot, in slot order; null when a slot names no preset or its file cannot be read, because a
        /// per-filament G-code value covers every slot and cannot be checked with one of them missing.
        /// </summary>
        private static IReadOnlyDictionary<string, string>?[] FilamentPresets(GcodeSettings settings, PrusaPresetLocator locator)
        {
            var names = ModifiedSettingsParser.ParseEntries(settings.Get("filament_settings_id"));
            return names.Select(name => name.Length == 0 ? null : locator.Read("filament", name)).ToArray();
        }

        /// <summary>
        /// Adds "key = value" for every key of the presets whose G-code value differs from what they saved. The presets
        /// are the slots of one type (a single print or printer preset, or one per filament), and the expected value
        /// is their saved values in slot order.
        /// </summary>
        private static void Compare(GcodeSettings settings, IReadOnlyList<IReadOnlyDictionary<string, string>?> presets, List<string> changes)
        {
            if (presets.Count == 0 || presets.Any(p => p == null))
            {
                return;
            }

            foreach (var key in presets[0]!.Keys)
            {
                if (IgnoredKeys.Contains(key) || !settings.TryGet(key, out var gcodeValue) || presets.Any(p => !p!.ContainsKey(key)))
                {
                    continue;
                }

                var saved = presets.Select(p => p![key]).ToList();
                if (!IsSameValue(gcodeValue, saved))
                {
                    changes.Add($"{key} = {gcodeValue}");
                }
            }
        }

        /// <summary>
        /// PrusaSlicer joins per-slot values with ',' for numbers and points and ';' for (possibly quoted) strings, so the
        /// G-code value matches when either split equals the saved slot values. One side may also be a single value
        /// the other repeats: a plain string setting is written once even on a multi-slot printer (filament_vendor),
        /// and a vendor bundle writes one per-extruder value (retract_lift = 0.2) that the G-code expands per extruder.
        /// A percent option may be written bare in a bundle (retract_before_wipe = 80) and as 80% in the G-code.
        /// </summary>
        private static bool IsSameValue(string gcodeValue, IReadOnlyList<string> savedPerPreset)
        {
            return SameElements(Quoted(gcodeValue), savedPerPreset.SelectMany(Quoted).ToList())
                || SameElements(Numeric(gcodeValue), savedPerPreset.SelectMany(Numeric).ToList());
        }

        private static IReadOnlyList<string> Quoted(string value) => ModifiedSettingsParser.ParseEntries(value);

        private static IReadOnlyList<string> Numeric(string value) => value.Split(',').Select(v => v.Trim()).ToList();

        private static bool SameElements(IReadOnlyList<string> gcode, IReadOnlyList<string> saved)
        {
            if (gcode.Count == saved.Count)
            {
                return gcode.Zip(saved).All(pair => SameElement(pair.First, pair.Second));
            }

            if (gcode.Count == 1 && saved.Count > 1)
            {
                return saved.All(v => SameElement(gcode[0], v));
            }

            return saved.Count == 1 && gcode.Count > 1 && gcode.All(v => SameElement(v, saved[0]));
        }

        private static bool SameElement(string gcode, string saved) =>
            gcode == saved || (gcode.EndsWith('%') && gcode[..^1] == saved);
    }
}
