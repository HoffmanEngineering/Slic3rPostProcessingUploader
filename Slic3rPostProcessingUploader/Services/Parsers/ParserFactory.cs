using Slic3rPostProcessingUploader.Services.Parsers.AnycubicSlicerNext;
using Slic3rPostProcessingUploader.Services.Parsers.BambuStudio;
using Slic3rPostProcessingUploader.Services.Parsers.FLSunSlicer;
using Slic3rPostProcessingUploader.Services.Parsers.OrcaSlicer;
using Slic3rPostProcessingUploader.Services.Parsers.PrusaSlicer;

namespace Slic3rPostProcessingUploader.Services.Parsers
{
    internal class ParserFactory
    {
        public static IGcodeParser GetParser(ArgumentParser arguments, TelemetryService telemetry, string gcode)
        {
            SendTemplateMetrics(arguments, telemetry);

            gcode = GcodeWindow.Trim(gcode);

            // search through the gcode to find the slicer used

            if (OrcaParser.IsOrcaSlicer(gcode))
            {
                return BuildOrcaParser(arguments);
            }
            else if (PrusaParser.IsPrusaSlicer(gcode))
            {
                return BuildPrusaParser(arguments);
            }
            else if (FLSunParser.IsFLSunSlicer(gcode))
            {
                return BuildFLSunParser(arguments);
            }
            else if (BambuStudioParser.IsBambuStudio(gcode))
            {
                return BuildBambuStudioParser(arguments);
            }
            else if (AnycubicSlicerNextParser.IsAnycubicSlicerNext(gcode))
            {
                return BuildAnycubicSlicerNextParser(arguments);
            }
            else
            {

                // If the slicer is not recognized, then try and parse using all of them and see which one matches more closely
                // This is a fallback mechanism in case the slicer is not recognized

                // Try Orca
                var orcaFullTemplate = new OrcaFullNoteTemplate().getNoteTemplate();
                var orcaParser = new OrcaParser(orcaFullTemplate);
                var orcaResults = orcaParser.CountTemplateMatches(gcode);
                var orcaPercentMatch = (double) orcaResults.numMatches / (double) orcaResults.numPlaceholders;

                telemetry.TrackEvent("OrcaPercentMatch", new Dictionary<string, object> { { "PercentMatch", orcaPercentMatch } });

                // Try Prusa
                var prusaFullTemplate = new PrusaFullNoteTemplate().getNoteTemplate();
                var prusaParser = new PrusaParser(prusaFullTemplate);
                var prusaResults = prusaParser.CountTemplateMatches(gcode);
                var PrusaPercentMatch = (double) prusaResults.numMatches / (double) prusaResults.numPlaceholders;

                telemetry.TrackEvent("PrusaPercentMatch", new Dictionary<string, object> { { "PercentMatch", PrusaPercentMatch } });

                // Try FL Sun
                var flsunFullTemplate = new FLSunFullNoteTemplate().getNoteTemplate();
                var flsunParser = new FLSunParser(flsunFullTemplate);
                var flsunResults = flsunParser.CountTemplateMatches(gcode);
                var flsunPercentMatch = (double) flsunResults.numMatches / (double) flsunResults.numPlaceholders;

                telemetry.TrackEvent("FLSunPercentMatch", new Dictionary<string, object> { { "PercentMatch", flsunPercentMatch } });

                // Try Bambu Studio
                var bambuStudioFullTemplate = new BambuStudioFullNoteTemplate().getNoteTemplate();
                var bambuStudioParser = new BambuStudioParser(bambuStudioFullTemplate);
                var bambuStudioResults = bambuStudioParser.CountTemplateMatches(gcode);
                var bambuStudioPercentMatch = (double)bambuStudioResults.numMatches / (double) bambuStudioResults.numPlaceholders;

                telemetry.TrackEvent("BambuStudioPercentMatch", new Dictionary<string, object> { { "PercentMatch", bambuStudioPercentMatch } });

                // Try Anycubic Slicer Next
                var anycubicSlicerNextFullTemplate = new AnycubicSlicerNextFullNoteTemplate().getNoteTemplate();
                var anycubicSlicerNextParser = new AnycubicSlicerNextParser(anycubicSlicerNextFullTemplate);
                var anycubicSlicerNextResults = anycubicSlicerNextParser.CountTemplateMatches(gcode);
                var anycubicSlicerNextPercentMatch = (double) anycubicSlicerNextResults.numMatches / (double) anycubicSlicerNextResults.numPlaceholders;

                telemetry.TrackEvent("AnycubicSlicerNextPercentMatch", new Dictionary<string, object> { { "PercentMatch", anycubicSlicerNextPercentMatch } });

                // Compare Results
                if (orcaPercentMatch > PrusaPercentMatch && orcaPercentMatch > flsunPercentMatch && orcaPercentMatch > bambuStudioPercentMatch && orcaPercentMatch > anycubicSlicerNextPercentMatch)
                {
                    return BuildOrcaParser(arguments);
                }
                else if (PrusaPercentMatch > orcaPercentMatch && PrusaPercentMatch > flsunPercentMatch && PrusaPercentMatch > bambuStudioPercentMatch && PrusaPercentMatch > anycubicSlicerNextPercentMatch)
                {
                    return BuildPrusaParser(arguments);
                }
                else if (flsunPercentMatch > orcaPercentMatch && flsunPercentMatch > PrusaPercentMatch && flsunPercentMatch > bambuStudioPercentMatch && flsunPercentMatch > anycubicSlicerNextPercentMatch)
                {
                    return BuildFLSunParser(arguments);
                }
                else if (bambuStudioPercentMatch > orcaPercentMatch && bambuStudioPercentMatch > PrusaPercentMatch && bambuStudioPercentMatch > flsunPercentMatch && bambuStudioPercentMatch > anycubicSlicerNextPercentMatch)
                {
                    return BuildBambuStudioParser(arguments);
                }
                else if (anycubicSlicerNextPercentMatch > orcaPercentMatch && anycubicSlicerNextPercentMatch > PrusaPercentMatch && anycubicSlicerNextPercentMatch > flsunPercentMatch && anycubicSlicerNextPercentMatch > bambuStudioPercentMatch)
                {
                    return BuildAnycubicSlicerNextParser(arguments);
                }
                else
                {
                    return BuildOrcaParser(arguments);
                }

            }
        }

        private static IGcodeParser BuildPrusaParser(ArgumentParser arguments)
        {
            INoteTemplate template = arguments.UseDefaultNoteTemplate
                            ? new PrusaDefaultNoteTemplate()
                            : arguments.UseFullNoteTemplate
                            ? new PrusaFullNoteTemplate()
                            : new NoteTemplateFromFile(arguments.NoteTemplatePath);

            return new PrusaParser(template.getNoteTemplate());
        }

        private static IGcodeParser BuildOrcaParser(ArgumentParser arguments)
        {
            INoteTemplate template = arguments.UseDefaultNoteTemplate
                            ? new OrcaDefaultNoteTemplate()
                            : arguments.UseFullNoteTemplate
                            ? new OrcaFullNoteTemplate()
                            : new NoteTemplateFromFile(arguments.NoteTemplatePath);

            return new OrcaParser(template.getNoteTemplate());
        }

        private static IGcodeParser BuildFLSunParser(ArgumentParser arguments)
        {
            INoteTemplate template = arguments.UseDefaultNoteTemplate
                            ? new FLSunDefaultNoteTemplate()
                            : arguments.UseFullNoteTemplate
                            ? new FLSunFullNoteTemplate()
                            : new NoteTemplateFromFile(arguments.NoteTemplatePath);
            return new FLSunParser(template.getNoteTemplate());
        }

        private static IGcodeParser BuildBambuStudioParser(ArgumentParser arguments)
        {
            INoteTemplate template = arguments.UseDefaultNoteTemplate
                            ? new BambuStudioDefaultNoteTemplate()
                            : arguments.UseFullNoteTemplate
                            ? new BambuStudioFullNoteTemplate()
                            : new NoteTemplateFromFile(arguments.NoteTemplatePath);
            return new BambuStudioParser(template.getNoteTemplate());
        }

        private static IGcodeParser BuildAnycubicSlicerNextParser(ArgumentParser arguments)
        {
            INoteTemplate template = arguments.UseDefaultNoteTemplate
                            ? new AnycubicSlicerNextDefaultNoteTemplate()
                            : arguments.UseFullNoteTemplate
                            ? new AnycubicSlicerNextFullNoteTemplate()
                            : new NoteTemplateFromFile(arguments.NoteTemplatePath);
            return new AnycubicSlicerNextParser(template.getNoteTemplate());
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
