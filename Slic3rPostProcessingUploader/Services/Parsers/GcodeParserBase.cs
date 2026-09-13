using System.Text.RegularExpressions;

namespace Slic3rPostProcessingUploader.Services.Parsers
{
    internal abstract partial class GcodeParserBase : IGcodeParser
    {
        [GeneratedRegex("{{(.*?)}}")]
        private static partial Regex TemplatePlaceholderRegex();

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
        /// Regex pattern to extract the slicer version from gcode.
        /// </summary>
        protected abstract string SlicerVersionPattern { get; }

        /// <summary>
        /// Creates the default note template for this slicer.
        /// </summary>
        protected abstract INoteTemplate CreateDefaultTemplate();

        /// <summary>
        /// The separator pattern used between setting name and value (default: "=").
        /// Override for slicers that use different separators (e.g., "[=:]" for BambuStudio).
        /// </summary>
        protected virtual string SettingSeparatorPattern => "=";

        /// <summary>
        /// The key used to find filament length in gcode (default: "filament used \\[mm\\]").
        /// </summary>
        protected virtual string FilamentLengthKey => "filament used \\[mm\\]";

        /// <summary>
        /// The key used to find filament weight in gcode (default: "filament used \\[g\\]").
        /// </summary>
        protected virtual string FilamentWeightKey => "filament used \\[g\\]";

        /// <summary>
        /// Whether this slicer supports multi-filament prints.
        /// </summary>
        protected virtual bool SupportsMultiFilament => false;

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

        public virtual CuraSettingDto ParseGcode(string gcode)
        {
            var dto = new CuraSettingDto();
            var settings = new CuraSettings();

            settings.estimated_print_time_seconds = ParseEstimatedPrintTime(gcode);

            if (SupportsMultiFilament)
            {
                settings.filamentUsage = GetFilamentUsage(gcode);
                if (settings.filamentUsage.Count == 0)
                {
                    settings.material_used_mg = (int?)EstimateFilamentUsageInMg(gcode);
                }
            }
            else
            {
                settings.material_used_mg = (int?)EstimateFilamentUsageInMg(gcode);
            }

            settings.note = RenderNoteTemplate(gcode);

            string? snapshot = GetSnapshot(gcode);
            if (snapshot != null)
            {
                settings.Snapshot = snapshot;
            }

            dto.Slicer = SlicerName;
            dto.CuraVersion = GetSlicerVersion(gcode);
            dto.settings = settings;

            return dto;
        }

        /// <summary>
        /// The note template will have placeholders that will be replaced with the actual values from the gcode.
        /// </summary>
        protected string RenderNoteTemplate(string gcode)
        {
            string template = noteTemplate;

            var matches = TemplatePlaceholderRegex().Matches(template);
            foreach (Match match in matches)
            {
                var placeholder = match.Groups[1].Value;
                string searchString = "; " + placeholder;
                var value = ParseSettingAsString(gcode, searchString);
                template = template.Replace(match.Value, value);
            }

            return template;
        }

        /// <summary>
        /// Count the number of placeholders in the note template that are found in the gcode. Used for heuristic matching.
        /// </summary>
        public (int numPlaceholders, int numMatches) CountTemplateMatches(string gcode)
        {
            int numPlaceholders = 0;
            int numMatches = 0;

            string template = this.noteTemplate;

            var matches = TemplatePlaceholderRegex().Matches(template);
            foreach (Match match in matches)
            {
                numPlaceholders++;

                var placeholder = match.Groups[1].Value;
                string searchString = "; " + placeholder;
                var value = ParseSettingAsString(gcode, searchString);

                if (!string.IsNullOrEmpty(value))
                {
                    numMatches++;
                }
            }

            return (numPlaceholders, numMatches);
        }

        protected string GetSlicerVersion(string gcode)
        {
            var regex = new Regex(SlicerVersionPattern, RegexOptions.IgnoreCase | RegexOptions.Multiline);
            var match = regex.Match(gcode);
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

        protected int ParseEstimatedPrintTime(string gcode)
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

                var normalModePrintTimeMatch = ParseSettingAsString(gcode, "; estimated printing time (normal mode)");
                if (!string.IsNullOrEmpty(normalModePrintTimeMatch))
                {
                    var time = ParseAsSeconds(normalModePrintTimeMatch);
                    if (time.HasValue)
                    {
                        return time.Value;
                    }
                }

                var silentMode = ParseSettingAsString(gcode, "; estimated printing time (silent mode)");
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
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                return 0;
            }
        }

        protected int? ParseAsSeconds(string input)
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

            // Try and parse a time formatted as 1h 35m 50s into seconds.
            var timeParts = input.Split(' ');
            var hours = timeParts.Where(x => x.Contains('h')).Select(x => int.Parse(x.Replace("h", ""))).FirstOrDefault();
            var minutes = timeParts.Where(x => x.Contains('m')).Select(x => int.Parse(x.Replace("m", ""))).FirstOrDefault();
            var seconds = timeParts.Where(x => x.Contains('s')).Select(x => int.Parse(x.Replace("s", ""))).FirstOrDefault();

            return hours * 3600 + minutes * 60 + seconds;
        }

        protected List<PrintFilamentSummaryDto> GetFilamentUsage(string gcode)
        {
            List<PrintFilamentSummaryDto> filament = new List<PrintFilamentSummaryDto>();

            string filamentUsed = ParseSettingAsString(gcode, "; " + FilamentLengthKey);

            if (string.IsNullOrEmpty(filamentUsed))
            {
                return filament;
            }

            List<double> usage = filamentUsed.Split(',')
                                             .Select(x => double.Parse(x.Trim()))
                                             .ToList();

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
                    Notes = string.Empty,
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

        public double? EstimateFilamentUsageInMg(string gcode)
        {
            if (string.IsNullOrEmpty(gcode))
            {
                return 0;
            }

            // Check to see if the user setup their filament densities, thus we can directly return filament usage.
            var filamentUsedInGrams = ParseSettingAsNumber(gcode, "; " + FilamentWeightKey);
            if (filamentUsedInGrams > 0)
            {
                return filamentUsedInGrams * 1000;
            }

            var filamentType = ParseSettingAsString(gcode, "; filament_type");
            // Try and grab the first diameter
            if (!double.TryParse(ParseSettingAsString(gcode, "; filament_diameter").Split(',')[0], out var filamentDiameter))
            {
                return 0;
            }
            if (!double.TryParse(ParseSettingAsString(gcode, "; " + FilamentLengthKey), out var filamentUsageLengthInMM))
            {
                return 0;
            }

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

        protected double ParseSettingAsNumber(string gcode, string settingName)
        {
            var regex = new Regex(settingName + " " + SettingSeparatorPattern + " (.+)$", RegexOptions.IgnoreCase | RegexOptions.Multiline);
            var match = regex.Match(gcode);
            if (match.Success)
            {
                return double.Parse(match.Groups[1].Value);
            }
            return double.NaN;
        }

        protected string ParseSettingAsString(string gcode, string settingName)
        {
            var regex = new Regex(settingName.Replace("(", "\\(").Replace(")", "\\)") + " " + SettingSeparatorPattern + " (.+)$", RegexOptions.IgnoreCase | RegexOptions.Multiline);
            var match = regex.Match(gcode);
            if (match.Success)
            {
                return match.Groups[1].Value.Trim();
            }
            return string.Empty;
        }
    }
}
