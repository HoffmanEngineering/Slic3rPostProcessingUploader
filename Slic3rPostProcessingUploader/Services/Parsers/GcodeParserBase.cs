using System.Globalization;
using System.Text.RegularExpressions;
using Slic3rPostProcessingUploader.Services.Parsers.Computed;

namespace Slic3rPostProcessingUploader.Services.Parsers
{
    internal abstract partial class GcodeParserBase : IGcodeParser
    {
        [GeneratedRegex("{{(.*?)}}")]
        private static partial Regex TemplatePlaceholderRegex();

        // Every placeholder, in one pass so a substituted value is never re-read as template. The first alternative is
        // a placeholder that is the only thing on its line apart from indentation: group 1 is the indentation, group 2
        // the key, group 3 the line terminator (empty at the end of the template). The lookbehind keeps a placeholder
        // that follows other text on the same line out of that branch; those match group 4 and render verbatim.
        [GeneratedRegex("(?<=^|\n)([ \t]*){{(.*?)}}[ \t]*(\r?\n|$)|{{(.*?)}}")]
        private static partial Regex RenderPlaceholderRegex();

        // Matches PNG ("thumbnail begin") and JPG ("thumbnail_JPG begin") blocks. QOI is deliberately excluded since browsers cannot render it.
        [GeneratedRegex("thumbnail(?:_JPG)? begin (\\d+)x(\\d+)[\\sa-zA-Z\\d]*([\\S\\s]*?); thumbnail end", RegexOptions.IgnoreCase | RegexOptions.Multiline)]
        private static partial Regex SnapshotRegex();

        /// <summary>
        /// Upper bound (720p) on the thumbnail resolution we will pick. Larger thumbnails are only used if nothing smaller exists.
        /// </summary>
        private const int MaxSnapshotWidth = 1280;
        private const int MaxSnapshotHeight = 720;

        [GeneratedRegex(@"total estimated time: (.+)$", RegexOptions.IgnoreCase | RegexOptions.Multiline)]
        private static partial Regex PrintTimeRegex();

        protected readonly string noteTemplate;

        /// <summary>
        /// The name of the slicer (e.g., "OrcaSlicer", "PrusaSlicer").
        /// </summary>
        protected abstract string SlicerName { get; }

        /// <summary>
        /// Regex that captures the slicer version from gcode in group 1. Implemented with <see cref="GeneratedRegexAttribute"/>
        /// so the pattern is compiled at build time rather than interpreted at runtime under <c>PublishAot</c>.
        /// </summary>
        protected abstract Regex SlicerVersionRegex { get; }

        /// <summary>
        /// Creates the default note template for this slicer.
        /// </summary>
        protected abstract INoteTemplate CreateDefaultTemplate();

        /// <summary>
        /// The characters accepted between setting name and value (default: "=").
        /// Override for slicers that use different separators (e.g., "=:" for BambuStudio).
        /// </summary>
        protected internal virtual ReadOnlySpan<char> SettingSeparators => "=";

        /// <summary>
        /// The key used to find filament length in gcode (default: "filament used [mm]").
        /// </summary>
        protected virtual string FilamentLengthKey => "filament used [mm]";

        /// <summary>
        /// The key used to find filament weight in gcode (default: "filament used [g]").
        /// </summary>
        protected virtual string FilamentWeightKey => "filament used [g]";

        /// <summary>
        /// Whether this slicer supports multi-filament prints.
        /// </summary>
        protected virtual bool SupportsMultiFilament => false;

        /// <summary>
        /// Values this slicer can compute from the G-code (e.g. object sizes) rather than read from a settings line.
        /// Each runs only when the note template references its key.
        /// </summary>
        protected virtual IReadOnlyList<ComputedPlaceholder> ComputedPlaceholders => [];

        protected GcodeParserBase(string? noteTemplate)
        {
            if (string.IsNullOrEmpty(noteTemplate))
            {
                var defaultTemplate = CreateDefaultTemplate();
                this.noteTemplate = defaultTemplate.getNoteTemplate();
            }
            else
            {
                this.noteTemplate = noteTemplate;
            }
        }

        public CuraSettingDto ParseGcode(string gcode) => ParseGcode(gcode, ParseOptions.InMemory(gcode));

        public virtual CuraSettingDto ParseGcode(string gcode, ParseOptions options)
        {
            // Slicer metadata only lives at the start and end of the file, so large files are narrowed down to those regions
            // and the "; key = value" lines are indexed once instead of scanning the file per setting.
            gcode = GcodeWindow.Trim(gcode);
            var gcodeSettings = GcodeSettings.Parse(gcode, SettingSeparators);
            RunComputedPlaceholders(gcodeSettings, options);

            var settings = new CuraSettings
            {
                estimated_print_time_seconds = ParseEstimatedPrintTime(gcode, gcodeSettings),
                note = RenderNoteTemplate(gcodeSettings),
                Snapshot = GetSnapshot(gcode),
            };

            if (SupportsMultiFilament)
            {
                settings.filamentUsage = GetFilamentUsage(gcodeSettings);
                if (settings.filamentUsage.Count == 0)
                {
                    settings.material_used_mg = (int?)EstimateFilamentUsageInMg(gcodeSettings);
                }
            }
            else
            {
                settings.material_used_mg = (int?)EstimateFilamentUsageInMg(gcodeSettings);
            }

            return new CuraSettingDto
            {
                Slicer = SlicerName,
                CuraVersion = GetSlicerVersion(gcode),
                settings = settings,
            };
        }

        /// <summary>
        /// Runs every computed placeholder the template references and stores its value in the settings index, where
        /// rendering picks it up like any other key. Unreferenced placeholders never run, so a template without
        /// {{models}} never pays for the full-file scan.
        /// </summary>
        private void RunComputedPlaceholders(GcodeSettings settings, ParseOptions options)
        {
            if (ComputedPlaceholders.Count == 0)
            {
                return;
            }

            var referencedKeys = TemplatePlaceholderRegex().Matches(noteTemplate)
                .Select(m => m.Groups[1].Value)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            ComputedContext? context = null;
            foreach (var placeholder in ComputedPlaceholders)
            {
                if (referencedKeys.Contains(placeholder.Key))
                {
                    context ??= new ComputedContext(settings, options);
                    settings.Set(placeholder.Key, RenderSafely(placeholder, context, options));
                }
            }
        }

        /// <summary>
        /// A placeholder promises not to throw, but a note section must never cost the user their print log, so this
        /// is the last line of defence: a throwing placeholder renders empty. The debug log is best effort here too,
        /// since a broken log file must not turn a swallowed failure into a fatal one.
        /// </summary>
        private static string RenderSafely(ComputedPlaceholder placeholder, ComputedContext context, ParseOptions options)
        {
            try
            {
                return placeholder.Render(context);
            }
            catch (Exception e)
            {
                try
                {
                    options.DebugLog($"{{{{{placeholder.Key}}}}} could not be computed and was left empty: {e}");
                }
                catch
                {
                    // Nothing left to report to.
                }

                return string.Empty;
            }
        }

        /// <summary>
        /// The note template will have placeholders that will be replaced with the actual values from the gcode.
        /// A placeholder that is alone on its line is laid out as a block: every line of its value gets the line's
        /// indentation, and an empty value removes the line altogether (the heading above it is left in place).
        /// </summary>
        protected string RenderNoteTemplate(GcodeSettings settings)
        {
            return RenderPlaceholderRegex().Replace(noteTemplate, match =>
            {
                if (match.Groups[4].Success)
                {
                    return settings.Get(match.Groups[4].Value);
                }

                var indent = match.Groups[1].Value;
                var value = settings.Get(match.Groups[2].Value);
                var terminator = match.Groups[3].Value;
                if (value.Length == 0)
                {
                    return string.Empty;
                }

                var separator = terminator.Length > 0 ? terminator : "\n";
                var lines = value.Split('\n').Select(line => indent + line.TrimEnd('\r'));
                return string.Join(separator, lines) + terminator;
            });
        }

        /// <summary>
        /// Count the number of placeholders in the note template that are found in the gcode. Used for heuristic matching.
        /// </summary>
        public (int numPlaceholders, int numMatches) CountTemplateMatches(string gcode)
        {
            return CountTemplateMatches(GcodeSettings.Parse(GcodeWindow.Trim(gcode), SettingSeparators));
        }

        /// <summary>
        /// Same as <see cref="CountTemplateMatches(string)"/> but against settings that were already indexed,
        /// so the factory can score several parsers without re-parsing the gcode for each one.
        /// </summary>
        public (int numPlaceholders, int numMatches) CountTemplateMatches(GcodeSettings settings)
        {
            int numPlaceholders = 0;
            int numMatches = 0;

            var computedKeys = ComputedPlaceholders.Select(p => p.Key).ToHashSet(StringComparer.OrdinalIgnoreCase);
            var matches = TemplatePlaceholderRegex().Matches(noteTemplate);
            foreach (Match match in matches)
            {
                // Computed keys are never "; key = value" lines, so they say nothing about which slicer wrote the file.
                if (computedKeys.Contains(match.Groups[1].Value))
                {
                    continue;
                }

                numPlaceholders++;

                var value = settings.Get(match.Groups[1].Value);

                if (!string.IsNullOrEmpty(value))
                {
                    numMatches++;
                }
            }

            return (numPlaceholders, numMatches);
        }

        /// <summary>
        /// Every distinct placeholder in the note template that has no value in the gcode, in template order.
        /// Used by tests to catch slicer releases that rename or drop a setting the built-in templates rely on.
        /// </summary>
        public IReadOnlyList<string> GetMissingPlaceholders(string gcode)
        {
            var settings = GcodeSettings.Parse(GcodeWindow.Trim(gcode), SettingSeparators);
            var seen = ComputedPlaceholders.Select(p => p.Key).ToHashSet(StringComparer.OrdinalIgnoreCase);
            var missing = new List<string>();

            // Computed keys start out in `seen`: their provider answers for them, not a settings line.
            foreach (Match match in TemplatePlaceholderRegex().Matches(noteTemplate))
            {
                var key = match.Groups[1].Value;
                if (seen.Add(key) && string.IsNullOrEmpty(settings.Get(key)))
                {
                    missing.Add(key);
                }
            }

            return missing;
        }

        protected string GetSlicerVersion(string gcode)
        {
            var match = SlicerVersionRegex.Match(gcode);
            if (match.Success)
            {
                return match.Groups[1].Value.Trim();
            }

            return "Unknown";
        }

        /// <summary>
        /// Slicers often embed several thumbnails (e.g. 48x48 and 300x300). Pick the highest resolution one up to 720p,
        /// falling back to the smallest available if every thumbnail exceeds 720p.
        /// </summary>
        protected string? GetSnapshot(string gcode)
        {
            var candidates = SnapshotRegex().Matches(gcode)
                .Select(m => new
                {
                    Width = int.Parse(m.Groups[1].Value),
                    Height = int.Parse(m.Groups[2].Value),
                    Payload = m.Groups[3].Value,
                })
                .ToList();

            if (candidates.Count == 0)
            {
                return null;
            }

            var withinLimit = candidates
                .Where(c => c.Width <= MaxSnapshotWidth && c.Height <= MaxSnapshotHeight)
                .ToList();

            var chosen = withinLimit.Count > 0
                ? withinLimit.MaxBy(c => c.Width * c.Height)!
                : candidates.MinBy(c => c.Width * c.Height)!;

            return chosen.Payload.Replace("\r\n; ", "").Replace("\n; ", "").Replace(";", "").Trim();
        }

        protected int ParseEstimatedPrintTime(string gcode, GcodeSettings settings)
        {
            try
            {
                var printTimeStringMatch = PrintTimeRegex().Match(gcode);
                if (printTimeStringMatch.Success)
                {
                    var time = ParseAsSeconds(printTimeStringMatch.Groups[1].Value);
                    if (time.HasValue)
                    {
                        return time.Value;
                    }
                }

                var normalModePrintTimeMatch = settings.Get("estimated printing time (normal mode)");
                if (!string.IsNullOrEmpty(normalModePrintTimeMatch))
                {
                    var time = ParseAsSeconds(normalModePrintTimeMatch);
                    if (time.HasValue)
                    {
                        return time.Value;
                    }
                }

                var silentMode = settings.Get("estimated printing time (silent mode)");
                if (!string.IsNullOrEmpty(silentMode))
                {
                    var time = ParseAsSeconds(silentMode);
                    if (time.HasValue)
                    {
                        return time.Value;
                    }
                }

                return 0;
            }
            catch (Exception)
            {
                return 0;
            }
        }

        internal int? ParseAsSeconds(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
            {
                return null;
            }

            if (int.TryParse(input, out var durationAsMs))
            {
                var durationAsSeconds = durationAsMs / 1000;
                return (int)Math.Floor((double)durationAsSeconds);
            }

            // Try and parse a time formatted as 1d 1h 35m 50s into seconds.
            var timeParts = input.Split(' ');
            var days = timeParts.Where(x => x.Contains('d')).Select(x => ParseIntOrZero(x.Replace("d", ""))).FirstOrDefault();
            var hours = timeParts.Where(x => x.Contains('h')).Select(x => ParseIntOrZero(x.Replace("h", ""))).FirstOrDefault();
            var minutes = timeParts.Where(x => x.Contains('m')).Select(x => ParseIntOrZero(x.Replace("m", ""))).FirstOrDefault();
            var seconds = timeParts.Where(x => x.Contains('s')).Select(x => ParseIntOrZero(x.Replace("s", ""))).FirstOrDefault();

            return days * 86400 + hours * 3600 + minutes * 60 + seconds;
        }

        private static int ParseIntOrZero(string value)
        {
            return int.TryParse(value, out var result) ? result : 0;
        }

        protected List<PrintFilamentSummaryDto> GetFilamentUsage(GcodeSettings settings)
        {
            List<PrintFilamentSummaryDto> filament = new List<PrintFilamentSummaryDto>();

            string filamentUsed = settings.Get(FilamentLengthKey);

            if (string.IsNullOrEmpty(filamentUsed))
            {
                return filament;
            }

            List<double> usage = ParseNumberList(filamentUsed);

            // Colours and types are per-slot lists aligned with the usage list. Slicers separate them with ';'.
            var colours = SplitFilamentList(settings.Get("filament_colour"));
            var types = SplitFilamentList(settings.Get("filament_type"));

            for (int i = 0; i < usage.Count; i++)
            {
                if (usage[i] == 0)
                {
                    continue;
                }

                PrintFilamentSummaryDto filamentUsage = new PrintFilamentSummaryDto
                {
                    EstimatedSource = PrintFilamentSourceMeasurement.Length,
                    EstimatedLengthInM = Math.Round(usage[i] / 1000, 3),
                    Id = null,
                    Notes = DescribeFilamentSlot(i, colours.ElementAtOrDefault(i), types.ElementAtOrDefault(i)),
                    Source = PrintFilamentSourceMeasurement.Length,
                    Filament = new FilamentSummary
                    {
                        DisplayName = "Other",
                        Id = "00000000-0000-0000-0000-000000000000"
                    }
                };

                filament.Add(filamentUsage);
            }

            return filament;
        }

        private static List<string> SplitFilamentList(string value)
        {
            // PrusaSlicer 3.x writes an unset string value as a pair of quotes, so strip them per entry.
            return value.Split([';', ','], StringSplitOptions.TrimEntries)
                        .Select(x => x.Trim('"'))
                        .ToList();
        }

        /// <summary>
        /// Builds a note like "Slot 1 · Red (#E72F1D) · PLA" so the user can tell which spool each usage entry belongs to,
        /// even when the printer has the filaments loaded in a different order than the slicer.
        /// </summary>
        protected static string DescribeFilamentSlot(int index, string? colour, string? type)
        {
            var parts = new List<string> { $"Slot {index + 1}" };

            if (!string.IsNullOrWhiteSpace(colour))
            {
                var name = FilamentColor.Describe(colour);
                parts.Add(name is null ? colour : $"{name} ({colour.ToUpperInvariant()})");
            }

            if (!string.IsNullOrWhiteSpace(type))
            {
                parts.Add(type);
            }

            return string.Join(" · ", parts);
        }

        public double? EstimateFilamentUsageInMg(GcodeSettings settings)
        {
            // Check to see if the user setup their filament densities, thus we can directly return filament usage.
            // Multi-extruder exports (e.g. PrusaSlicer MMU/XL) write one value per slot, so the slots are summed.
            var filamentUsedInGrams = ParseNumberList(settings.Get(FilamentWeightKey)).Sum();
            if (filamentUsedInGrams > 0)
            {
                return filamentUsedInGrams * 1000;
            }

            var filamentType = settings.Get("filament_type");
            // Try and grab the first diameter
            var filamentDiameter = ParseNumberList(settings.Get("filament_diameter")).FirstOrDefault();
            if (filamentDiameter <= 0)
            {
                return 0;
            }

            // This path only runs when the per-slot breakdown is unavailable, so the first slot's length is used.
            var filamentUsageLengthInMM = ParseNumberList(settings.Get(FilamentLengthKey)).FirstOrDefault();
            if (filamentUsageLengthInMM <= 0)
            {
                return 0;
            }

            // Every supported slicer writes the density the user configured for the loaded filament(s), so prefer
            // that over guessing from the material name. Multi-filament prints write a comma-separated list; the
            // first slot is used here to match the length above.
            var filamentDensity = ParseNumberList(settings.Get("filament_density")).FirstOrDefault();
            if (filamentDensity > 0)
            {
                return CalculateWeightInMg(filamentDensity, filamentUsageLengthInMM, filamentDiameter);
            }

            // Last-resort fallback for gcode that has no filament_density: a small table of common materials.
            if (filamentType.Contains("PLA"))
            {
                return CalculateWeightInMg(MaterialDensities.Materials.PLA, filamentUsageLengthInMM, filamentDiameter);
            }
            else if (filamentType.Contains("ABS"))
            {
                return CalculateWeightInMg(MaterialDensities.Materials.ABS, filamentUsageLengthInMM, filamentDiameter);
            }
            else if (filamentType.Contains("PETG"))
            {
                return CalculateWeightInMg(MaterialDensities.Materials.PETG, filamentUsageLengthInMM, filamentDiameter);
            }

            return 0;
        }

        protected double CalculateWeightInMg(double materialDensityGramsPerCubicCm, double lengthInMm, double diameterInMm)
        {
            var radiusInMm = diameterInMm / 2;
            var filamentAreaInMm2 = Math.PI * Math.Pow(radiusInMm, 2);

            var volume = filamentAreaInMm2 * lengthInMm;

            var densityInCubicMm = materialDensityGramsPerCubicCm / 1000;

            var weightInGrams = volume * densityInCubicMm;
            return Math.Floor(weightInGrams * 1000);
        }

        /// <summary>
        /// Parses a comma-separated list of numbers such as "4.53, 0.00, 2.51". Entries that are not numeric become 0
        /// so a malformed line never throws; an empty value yields an empty list.
        /// </summary>
        protected static List<double> ParseNumberList(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return [];
            }

            return value.Split(',', StringSplitOptions.TrimEntries)
                        .Select(x => double.TryParse(x, NumberStyles.Float, CultureInfo.InvariantCulture, out var number) ? number : 0)
                        .ToList();
        }
    }
}
