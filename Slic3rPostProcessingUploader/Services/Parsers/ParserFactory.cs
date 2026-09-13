using Slic3rPostProcessingUploader.Services.Parsers.AnycubicSlicerNext;
using Slic3rPostProcessingUploader.Services.Parsers.BambuStudio;
using Slic3rPostProcessingUploader.Services.Parsers.FLSunSlicer;
using Slic3rPostProcessingUploader.Services.Parsers.OrcaSlicer;
using Slic3rPostProcessingUploader.Services.Parsers.PrusaSlicer;

namespace Slic3rPostProcessingUploader.Services.Parsers
{
    /// <summary>
    /// Everything the factory needs to know about one slicer. <paramref name="Name"/> is used in telemetry event names.
    /// </summary>
    internal sealed record SlicerRegistration(
        string Name,
        Func<string, bool> Detect,
        Func<INoteTemplate> DefaultTemplate,
        Func<INoteTemplate> FullTemplate,
        Func<string?, GcodeParserBase> Create);

    internal class ParserFactory
    {
        /// <summary>
        /// Registry order matters: it is the detection order, and the heuristic fallback resolves ties in favour of
        /// the earlier entry (so Orca is the default when nothing matches at all).
        /// </summary>
        private static readonly SlicerRegistration[] Slicers =
        [
            new("Orca", OrcaParser.IsOrcaSlicer, () => new OrcaDefaultNoteTemplate(), () => new OrcaFullNoteTemplate(), t => new OrcaParser(t)),
            new("Prusa", PrusaParser.IsPrusaSlicer, () => new PrusaDefaultNoteTemplate(), () => new PrusaFullNoteTemplate(), t => new PrusaParser(t)),
            new("FLSun", FLSunParser.IsFLSunSlicer, () => new FLSunDefaultNoteTemplate(), () => new FLSunFullNoteTemplate(), t => new FLSunParser(t)),
            new("BambuStudio", BambuStudioParser.IsBambuStudio, () => new BambuStudioDefaultNoteTemplate(), () => new BambuStudioFullNoteTemplate(), t => new BambuStudioParser(t)),
            new("AnycubicSlicerNext", AnycubicSlicerNextParser.IsAnycubicSlicerNext, () => new AnycubicSlicerNextDefaultNoteTemplate(), () => new AnycubicSlicerNextFullNoteTemplate(), t => new AnycubicSlicerNextParser(t)),
        ];

        public static IGcodeParser GetParser(ArgumentParser arguments, TelemetryService telemetry, ConsoleOutput output, string gcode)
        {
            SendTemplateMetrics(arguments, telemetry);

            gcode = GcodeWindow.Trim(gcode);

            var slicer = Slicers.FirstOrDefault(s => s.Detect(gcode))
                ?? FindClosestMatch(gcode, telemetry, output);

            return BuildParser(slicer, arguments);
        }

        /// <summary>
        /// Fallback for gcode without a recognized slicer marker: score every slicer's full template against the
        /// gcode and pick the one with the highest fraction of placeholders that have a value.
        /// </summary>
        private static SlicerRegistration FindClosestMatch(string gcode, TelemetryService telemetry, ConsoleOutput output)
        {
            output.Warn("Slicer not recognized, using the closest matching parser. Some settings may be missing.");

            // Parsers differ only in the separators they accept, so the gcode is indexed once per distinct separator set.
            var settingsBySeparators = new Dictionary<string, GcodeSettings>();

            return Slicers.MaxBy(slicer =>
            {
                var parser = slicer.Create(slicer.FullTemplate().getNoteTemplate());
                var separators = parser.SettingSeparators.ToString();
                if (!settingsBySeparators.TryGetValue(separators, out var settings))
                {
                    settings = GcodeSettings.Parse(gcode, separators);
                    settingsBySeparators[separators] = settings;
                }

                var (numPlaceholders, numMatches) = parser.CountTemplateMatches(settings);
                var percentMatch = (double)numMatches / numPlaceholders;

                telemetry.TrackEvent($"{slicer.Name}PercentMatch", new Dictionary<string, object> { { "PercentMatch", percentMatch } });

                return percentMatch;
            })!;
        }

        private static IGcodeParser BuildParser(SlicerRegistration slicer, ArgumentParser arguments)
        {
            INoteTemplate template;
            if (arguments.UseDefaultNoteTemplate)
            {
                template = slicer.DefaultTemplate();
            }
            else if (arguments.UseFullNoteTemplate)
            {
                template = slicer.FullTemplate();
            }
            else if (!string.IsNullOrEmpty(arguments.NoteTemplatePath))
            {
                template = new NoteTemplateFromFile(arguments.NoteTemplatePath);
            }
            else
            {
                throw new UserFacingException(
                    "No note template was selected",
                    "Pass --default, --full, or --template <path> to choose which note template to use.");
            }

            return slicer.Create(template.getNoteTemplate());
        }

        private static void SendTemplateMetrics(ArgumentParser arguments, TelemetryService telemetry)
        {
            // Track the template used as an event
            if (arguments.UseDefaultNoteTemplate)
            {
                telemetry.TrackEvent("Template", new Dictionary<string, object> { { "Template", "Default" } });
            }
            else if (arguments.UseFullNoteTemplate)
            {
                telemetry.TrackEvent("Template", new Dictionary<string, object> { { "Template", "Full" } });
            }
            else
            {
                telemetry.TrackEvent("Template", new Dictionary<string, object> { { "Template", "Custom" } });
            }
        }
    }
}
