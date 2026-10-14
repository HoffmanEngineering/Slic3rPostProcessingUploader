using PrintLog.SlicerSettings;

namespace Slic3rPostProcessingUploader.Services
{
    /// <summary>
    /// The uploader's side of parser selection: detection itself lives in <see cref="SlicerDetector"/>; this adds the
    /// template the command line asked for, the fallback warning and the telemetry events.
    /// </summary>
    internal class ParserFactory
    {
        public static IGcodeParser GetParser(ArgumentParser arguments, TelemetryService telemetry, ConsoleOutput output, string gcode)
        {
            SendTemplateMetrics(arguments, telemetry);

            var slicer = SlicerDetector.Detect(
                gcode,
                (name, percentMatch) => telemetry.TrackEvent($"{name}PercentMatch", new Dictionary<string, object> { { "PercentMatch", percentMatch } }));

            if (!slicer.IsRecognized)
            {
                output.Warn("Slicer not recognized, using the closest matching parser. Some settings may be missing.");
            }

            return BuildParser(slicer, arguments);
        }

        private static IGcodeParser BuildParser(DetectedSlicer slicer, ArgumentParser arguments)
        {
            INoteTemplate template;
            if (arguments.UseDefaultNoteTemplate)
            {
                template = slicer.DefaultTemplate;
            }
            else if (arguments.UseFullNoteTemplate)
            {
                template = slicer.FullTemplate;
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

            return slicer.CreateParser(template.getNoteTemplate());
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
