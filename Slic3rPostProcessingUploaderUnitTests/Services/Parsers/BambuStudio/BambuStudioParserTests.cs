using Slic3rPostProcessingUploader.Services.Parsers.BambuStudio;
using Snapshooter.MSTest;

namespace Slic3rPostProcessingUploaderUnitTests.Services.Parsers.BambuStudio;

[TestClass]
public sealed class BambuStudioParserTests
{
    private static string CalibrationCube => TestData.Load(Path.Combine("BambuStudio", "bambustudio-01.10.01.50-calibration-cube.gcode"));
    private static string TwoFilamentCalibrationCube => TestData.Load(Path.Combine("BambuStudio", "bambustudio-01.10.01.50-calibration-cube-two-filament.gcode"));

    public static IEnumerable<object[]> Fixtures =>
        TestData.EnumerateFixtures("BambuStudio").Select(path => new object[] { path });

    [TestMethod]
    public void ShouldReturnAnEmptySettingWhenGcodeIsEmpty()
    {
        var parser = new BambuStudioParser("");
        var result = parser.ParseGcode("");
        Assert.IsNotNull(result);

        Snapshot.Match(result);
    }

    [TestMethod]
    [DynamicData(nameof(Fixtures))]
    public void EveryFixture_IsRecognizedAndParses(string fixturePath)
    {
        ParserFixtureAssertions.AssertRecognizedAndSnapshot<BambuStudioParser>(fixturePath);
    }

    [TestMethod]
    public void ShouldRenderTheExpectedNoteWhenGivenATemplateWithNoReplacements()
    {
        string template = "Settings:";

        var parser = new BambuStudioParser(template);
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

        var parser = new BambuStudioParser(template);
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
                First Layer Height: {{initial_layer_print_height}}
                Wall Loops: {{wall_loops}}
                Top Shell Layers: {{top_shell_layers}}
                Bottom Shell Layers: {{bottom_shell_layers}}
                Sparse Infill Density: {{sparse_infill_density}}
            """;

        var parser = new BambuStudioParser(template);
        var result = parser.ParseGcode(CalibrationCube);

        Assert.AreEqual("""
            Settings:
                Layer Height: 0.2
                First Layer Height: 0.28
                Wall Loops: 3
                Top Shell Layers: 3
                Bottom Shell Layers: 3
                Sparse Infill Density: 10%
            """, result.settings.note);
    }

    [TestMethod]
    public void ShouldRenderFullTemplateWhenGivenAGcodeWithTwoFilaments()
    {
        var parser = new BambuStudioParser("");
        var result = parser.ParseGcode(TwoFilamentCalibrationCube);

        Snapshot.Match(result, matchOptions => matchOptions.HashField("settings.Snapshot"));
    }
}
