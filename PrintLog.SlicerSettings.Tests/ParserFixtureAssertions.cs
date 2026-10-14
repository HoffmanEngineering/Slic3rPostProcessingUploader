using Snapshooter.MSTest;

namespace PrintLog.SlicerSettings.Tests;

internal static class ParserFixtureAssertions
{
    public static void AssertRecognizedAndSnapshot<TParser>(string fixturePath)
        where TParser : IGcodeParser
    {
        var gcode = TestData.Load(fixturePath);

        // Recognized means the slicer's marker matched, so the uploader shows no "Slicer not recognized" warning.
        var slicer = SlicerDetector.Detect(gcode);
        var parser = slicer.CreateParser(slicer.DefaultTemplate.getNoteTemplate());

        Assert.AreEqual(typeof(TParser), parser.GetType());
        Assert.IsTrue(slicer.IsRecognized, $"{fixturePath} should be recognized by its slicer marker");

        var result = parser.ParseGcode(gcode);
        Snapshot.Match(
            result,
            Path.GetFileNameWithoutExtension(fixturePath),
            matchOptions => matchOptions.HashField("settings.Snapshot"));
    }
}
