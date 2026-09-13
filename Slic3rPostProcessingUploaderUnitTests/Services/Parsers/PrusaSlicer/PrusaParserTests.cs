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
            ? new PrusaDefaultNoteTemplate().getNoteTemplate()
            : new PrusaFullNoteTemplate().getNoteTemplate();
        string gcode = slicerVersion == "PrusaSlicer2"
            ? PrusaSlicer2Cube
            : PrusaSlicer3Cube;

        var parser = new PrusaParser(template);
        var (numPlaceholders, numMatches) = parser.CountTemplateMatches(gcode);

        Assert.AreEqual(numPlaceholders, numMatches, $"{numPlaceholders - numMatches} placeholder(s) in the {templateName} template are not present in {slicerVersion} gcode");
    }
}
