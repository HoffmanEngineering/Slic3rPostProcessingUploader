using Slic3rPostProcessingUploader.Services;
using Slic3rPostProcessingUploader.Services.Parsers;

namespace Slic3rPostProcessingUploaderUnitTests.Services
{
    [TestClass]
    public class DryRunReportTests
    {
        private StringWriter _screen = null!;
        private ConsoleOutput _output = null!;

        [TestInitialize]
        public void Setup()
        {
            _screen = new StringWriter();
            _output = new ConsoleOutput(_screen, null, verbose: false, unicode: false, useColor: false);
        }

        private static CuraSettingDto BuildDto() => new()
        {
            Slicer = "OrcaSlicer",
            CuraVersion = "2.2.0",
            PluginVersion = "1.0.0",
            settings = new CuraSettings
            {
                note = "Layer height: 0.2\nInfill: 15%",
                print_name = "calibration-cube",
                file_name = "calibration-cube.gcode",
                estimated_print_time_seconds = 3723,
                material_used_mg = 12345,
                Snapshot = new string('A', 4000),
                filamentUsage =
                [
                    new PrintFilamentSummaryDto
                    {
                        Filament = new FilamentSummary { Id = "00000000-0000-0000-0000-000000000000", DisplayName = "Other" },
                        EstimatedLengthInM = 1.52,
                        EstimatedAmountMg = 4500,
                        Notes = "Slot 1 · Teal (#26A69A) · PLA"
                    },
                    new PrintFilamentSummaryDto
                    {
                        Filament = new FilamentSummary { Id = "00000000-0000-0000-0000-000000000000", DisplayName = "Other" },
                        EstimatedLengthInM = 0.5,
                        Notes = "Slot 2 · Red (#FF0000) · PETG"
                    }
                ]
            }
        };

        [TestMethod]
        public void Write_PrintsKeyFields()
        {
            DryRunReport.Write(_output, BuildDto(), debugPath: null);

            string text = _screen.ToString();
            StringAssert.Contains(text, "Dry run: nothing was uploaded and no browser was opened");
            StringAssert.Contains(text, "Slicer: OrcaSlicer 2.2.0");
            StringAssert.Contains(text, "Print name: calibration-cube");
            StringAssert.Contains(text, "Estimated print time: 1h 2m 3s (3723 s)");
            StringAssert.Contains(text, "Material used: 12.3 g");
            StringAssert.Contains(text, "Filaments: 2");
            StringAssert.Contains(text, "Slot 1 · Teal (#26A69A) · PLA: 1.52 m, 4.5 g");
            StringAssert.Contains(text, "Slot 2 · Red (#FF0000) · PETG: 0.5 m");
            StringAssert.Contains(text, "Thumbnail: found (4000 base64 chars, ~2.9 KB)");
        }

        [TestMethod]
        public void Write_PrintsRenderedNoteVerbatim()
        {
            DryRunReport.Write(_output, BuildDto(), debugPath: null);

            string text = _screen.ToString();
            StringAssert.Contains(text, "Rendered note:");
            StringAssert.Contains(text, Environment.NewLine + "Layer height: 0.2\nInfill: 15%" + Environment.NewLine);
        }

        [TestMethod]
        public void Write_WithoutDebugPath_PrintsDtoJsonToConsole()
        {
            var dto = BuildDto();

            DryRunReport.Write(_output, dto, debugPath: null);

            string text = _screen.ToString();
            StringAssert.Contains(text, "DTO JSON:");
            StringAssert.Contains(text, dto.ToJSON());
        }

        [TestMethod]
        public void Write_WithDebugPath_PointsAtDtoFileInsteadOfPrintingJson()
        {
            var dto = BuildDto();

            DryRunReport.Write(_output, dto, debugPath: "C:\\debug");

            string text = _screen.ToString();
            StringAssert.Contains(text, "DTO JSON written to " + Path.Combine("C:\\debug", "slic3r-dto.json"));
            Assert.IsFalse(text.Contains("\"pluginVersion\""), "DTO JSON should not be printed to the console when --debug is given");
        }

        [TestMethod]
        public void Write_WithMissingOptionalFields_ReportsThemAsMissing()
        {
            var dto = new CuraSettingDto
            {
                Slicer = "PrusaSlicer",
                CuraVersion = "2.9.2",
                PluginVersion = "1.0.0",
                settings = new CuraSettings
                {
                    note = "",
                    estimated_print_time_seconds = 45,
                    material_used_mg = null,
                    Snapshot = null,
                    filamentUsage = null
                }
            };

            DryRunReport.Write(_output, dto, debugPath: null);

            string text = _screen.ToString();
            StringAssert.Contains(text, "Estimated print time: 45s (45 s)");
            StringAssert.Contains(text, "Material used: not found");
            StringAssert.Contains(text, "Filaments: none");
            StringAssert.Contains(text, "Thumbnail: not found");
        }
    }
}
