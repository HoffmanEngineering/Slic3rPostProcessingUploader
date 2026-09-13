using Slic3rPostProcessingUploader.Services.Parsers.AnycubicSlicerNext;
using Snapshooter.MSTest;

namespace Slic3rPostProcessingUploaderUnitTests.Services.Parsers.AnycubicSlicerNext;

[TestClass]
public sealed class AnycubicSlicerNextParserTests
{
    private static string CalibrationCube => TestData.Load(Path.Combine("AnycubicSlicerNext", "anycubicslicernext-1.3.2-calibration-cube.gcode"));
    private static string TwoFilamentCalibrationCube => TestData.Load(Path.Combine("AnycubicSlicerNext", "anycubicslicernext-1.3.2-calibration-cube-two-filament.gcode"));

    public static IEnumerable<object[]> Fixtures =>
        TestData.EnumerateFixtures("AnycubicSlicerNext").Select(path => new object[] { path });

    [TestMethod]
    public void ShouldReturnAnEmptySettingWhenGcodeIsEmpty()
    {
        var parser = new AnycubicSlicerNextParser("");
        var result = parser.ParseGcode("");
        Assert.IsNotNull(result);

        Snapshot.Match(result, matchOptions => matchOptions.HashField("settings.Snapshot"));
    }

    [TestMethod]
    [DynamicData(nameof(Fixtures))]
    public void EveryFixture_IsRecognizedAndParses(string fixturePath)
    {
        ParserFixtureAssertions.AssertRecognizedAndSnapshot<AnycubicSlicerNextParser>(fixturePath);
    }

    [TestMethod]
    public void ShouldRenderTheExpectedNoteWhenGivenATemplateWithNoReplacements()
    {
        string template = "Settings:";

        var parser = new AnycubicSlicerNextParser(template);
        var result = parser.ParseGcode(CalibrationCube);

        Assert.AreEqual("Settings:", result.settings.note);
    }

    [TestMethod]
    public void ShouldRenderTheExpectedNoteWhenGivenATemplateWithASingleReplacement()
    {
        string template = """
            Settings:
                Layer Height: {{layer_height}}
            """;

        var parser = new AnycubicSlicerNextParser(template);
        var result = parser.ParseGcode(CalibrationCube);

        Assert.AreEqual("""
            Settings:
                Layer Height: 0.2
            """, result.settings.note);
    }

    [TestMethod]
    public void ShouldRenderTheExpectedNoteWhenGivenATemplateWithMultipleReplacements()
    {
        string template = """
            Settings:
                Layer Height: {{layer_height}}
                First Layer Height: {{first_layer_height}}
                Wall Loops: {{wall_loops}}
                Top Shell Layers: {{top_shell_layers}}
                Bottom Shell Layers: {{bottom_shell_layers}}
                Sparse Infill Density: {{sparse_infill_density}}
            """;

        var parser = new AnycubicSlicerNextParser(template);
        var result = parser.ParseGcode(CalibrationCube);

        Assert.AreEqual("""
            Settings:
                Layer Height: 0.2
                First Layer Height: 0.200
                Wall Loops: 2
                Top Shell Layers: 5
                Bottom Shell Layers: 3
                Sparse Infill Density: 15%
            """, result.settings.note);
    }

    [TestMethod]
    public void ShouldRenderFullTemplateWhenGivenAGcodeWithTwoFilaments()
    {
        var parser = new AnycubicSlicerNextParser("");
        var result = parser.ParseGcode(TwoFilamentCalibrationCube);

        Snapshot.Match(result, matchOptions => matchOptions.HashField("settings.Snapshot"));
    }
}
