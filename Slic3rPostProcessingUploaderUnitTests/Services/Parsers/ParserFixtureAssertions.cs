using Slic3rPostProcessingUploader.Services;
using Slic3rPostProcessingUploader.Services.Parsers;
using Snapshooter.MSTest;

namespace Slic3rPostProcessingUploaderUnitTests.Services.Parsers;

internal static class ParserFixtureAssertions
{
    public static void AssertRecognizedAndSnapshot<TParser>(string fixturePath)
        where TParser : IGcodeParser
    {
        var gcode = TestData.Load(fixturePath);
        using var screen = new StringWriter();
        var output = new ConsoleOutput(screen, null, verbose: false, unicode: true, useColor: false);
        using var telemetry = new TelemetryService(disableTelemetry: true);

        var parser = ParserFactory.GetParser(new ArgumentParser(["--default"]), telemetry, output, gcode);

        Assert.AreEqual(typeof(TParser), parser.GetType());
        Assert.AreEqual(string.Empty, screen.ToString());

        var result = parser.ParseGcode(gcode);
        Snapshot.Match(
            result,
            Path.GetFileNameWithoutExtension(fixturePath),
            matchOptions => matchOptions.HashField("settings.Snapshot"));
    }
}
