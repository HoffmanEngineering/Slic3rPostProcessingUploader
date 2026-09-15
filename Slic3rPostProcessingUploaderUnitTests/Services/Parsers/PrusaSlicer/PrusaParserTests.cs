using Slic3rPostProcessingUploader.Services;
using Slic3rPostProcessingUploader.Services.Parsers.PrusaSlicer;
using Snapshooter.MSTest;

namespace Slic3rPostProcessingUploaderUnitTests.Services.Parsers.PrusaSlicer;

[TestClass]
public sealed class PrusaParserTests
{
    private static string PrusaSlicer2Cube => TestData.Load(Path.Combine("PrusaSlicer", "prusaslicer-2.9.2-calibration-cube.gcode"));
    private static string PrusaSlicer3Cube => TestData.Load(Path.Combine("PrusaSlicer", "prusaslicer-3.0.0-alpha11-calibration-cube.gcode"));

    public static IEnumerable<object[]> Fixtures =>
        TestData.EnumerateFixtures("PrusaSlicer").Select(path => new object[] { path });

    [TestMethod]
    public void ShouldReturnAnEmptySettingWhenGcodeIsEmpty()
    {
        var parser = new PrusaParser("");
        var result = parser.ParseGcode("");
        Assert.IsNotNull(result);

        Snapshot.Match(result, matchOptions => matchOptions.HashField("settings.Snapshot"));
    }

    [TestMethod]
    public void ShouldDetectPrusaSlicer2And3Gcode()
    {
        Assert.IsTrue(PrusaParser.IsPrusaSlicer(PrusaSlicer2Cube));
        Assert.IsTrue(PrusaParser.IsPrusaSlicer(PrusaSlicer3Cube));
    }

    [TestMethod]
    [DynamicData(nameof(Fixtures))]
    public void EveryFixture_IsRecognizedAndParses(string fixturePath)
    {
        ParserFixtureAssertions.AssertRecognizedAndSnapshot<PrusaParser>(fixturePath);
    }

    [TestMethod]
    public void ShouldRenderEnumSupportMaterialFromPrusaSlicer3()
    {
        var parser = new PrusaParser("Generate Support: {{support_material}}");
        var result = parser.ParseGcode(PrusaSlicer3Cube);

        Assert.AreEqual("Generate Support: enforcers_only", result.settings.note);
    }

    [TestMethod]
    [DataRow("Default", "PrusaSlicer2")]
    [DataRow("Default", "PrusaSlicer3")]
    [DataRow("Full", "PrusaSlicer2")]
    [DataRow("Full", "PrusaSlicer3")]
    public void EveryTemplatePlaceholderShouldResolveAgainstRealGcode(string templateName, string slicerVersion)
    {
        string template = templateName == "Default"
            ? EmbeddedNoteTemplate.Default("PrusaSlicer").getNoteTemplate()
            : EmbeddedNoteTemplate.Full("PrusaSlicer").getNoteTemplate();
        string gcode = slicerVersion == "PrusaSlicer2"
            ? PrusaSlicer2Cube
            : PrusaSlicer3Cube;

        // Both exports are real files whose preset ids are legitimately blank: the 2.9.2 cube was sliced on a custom
        // printer with no vendor model, the 3.0.0-alpha11 cube with unsaved (unnamed) print and printer presets.
        string[] knownEmpty = slicerVersion == "PrusaSlicer2"
            ? ["printer_model"]
            : ["print_settings_id", "printer_settings_id"];

        var parser = new PrusaParser(template);
        var missing = parser.GetMissingPlaceholders(gcode).Except(knownEmpty, StringComparer.Ordinal).ToList();

        Assert.AreEqual(0, missing.Count, $"{missing.Count} placeholder(s) in the {templateName} template are not present in {slicerVersion} gcode: {string.Join(", ", missing)}");
    }

    [TestMethod]
    public void ShouldReportPerSlotFilamentUsageForMultiExtruderExport()
    {
        var gcode = TestData.Load(Path.Combine("PrusaSlicer", "prusaslicer-3.0.0-alpha11-xl5t-two-slots.gcode"));

        var result = new PrusaParser("").ParseGcode(gcode);

        Assert.IsNull(result.settings.material_used_mg);
        var filamentUsage = result.settings.filamentUsage!;
        var notes = filamentUsage.Select(f => f.Notes).ToList();
        CollectionAssert.AreEqual(new[]
        {
            "Slot 1 · Red (#E72F1D) · PLA",
            "Slot 3 · Blue (#1F77B4) · PLA",
        }, notes);
        Assert.AreEqual(1.363, filamentUsage[0].EstimatedLengthInM);
        Assert.AreEqual(1.613, filamentUsage[1].EstimatedLengthInM);
    }

    [TestMethod]
    public void ShouldReportSingleSlotWhenOnlyOneExtruderIsUsedOnMultiExtruderPrinter()
    {
        var gcode = TestData.Load(Path.Combine("PrusaSlicer", "prusaslicer-3.0.0-alpha11-xl5t-single-slot.gcode"));

        var result = new PrusaParser("").ParseGcode(gcode);

        Assert.AreEqual(1, result.settings.filamentUsage!.Count);
        Assert.AreEqual("Slot 1 · Red (#E72F1D) · PLA", result.settings.filamentUsage[0].Notes);
    }

    [TestMethod]
    public void ShouldRenderTheComputedSectionsFromAnMmuExport()
    {
        // PrusaSlicer writes filament_vendor once for every slot. The fixture keeps no whole layers, so there are no
        // objects to measure, and in-memory parsing has no config roots, so no preset can be checked: both sections
        // vanish with their line.
        var gcode = TestData.Load(Path.Combine("PrusaSlicer", "prusaslicer-2.9.2-mk4s-mmu3-two-slots-pla-petg.gcode"));

        var result = new PrusaParser("{{filament_profiles}}\n{{models}}\n{{modified_settings}}\nEnd").ParseGcode(gcode);

        Assert.AreEqual("Prusament PLA @MK4S (PLA, Prusa Polymers) ×4, Prusament PETG @MK4S (PETG, Prusa Polymers)\nEnd", result.settings.note);
    }

    [TestMethod]
    public void ShouldMeasureFirmwareLabelledObjectsFromARealExport()
    {
        // Firmware labelling (M486): the fixture keeps layers 1, 2, 124 and 125 of a 25 mm box and a 28.2 mm cylinder.
        // The sizes match the footprints PrusaSlicer wrote in its own objects_info line and max_layer_z = 25.
        var gcode = TestData.Load(Path.Combine("PrusaSlicer", "prusaslicer-2.9.2-mk4s-mmu3-shape-box-cylinder.gcode"));

        var result = new PrusaParser("{{models}}").ParseGcode(gcode);

        Assert.AreEqual(
            "Models:\n" +
            "  Shape-Cylinder  ×1   28.2 × 28.2 × 25.0 mm\n" +
            "  Shape-Box       ×1   25.0 × 25.0 × 25.0 mm\n",
            result.settings.note);
    }
}
