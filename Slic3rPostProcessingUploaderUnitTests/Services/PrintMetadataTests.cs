using Slic3rPostProcessingUploader.Services;
using Slic3rPostProcessingUploader.Services.Parsers;

namespace Slic3rPostProcessingUploaderUnitTests.Services;

[TestClass]
public sealed class PrintMetadataTests
{
    private static readonly string InputFile = Path.Combine(Path.GetTempPath(), "slicer-tmp", "calibration_cube.gcode");

    private static CuraSettingDto ParsedDto() => new()
    {
        Slicer = "PrusaSlicer",
        CuraVersion = "2.8.1",
        settings = new CuraSettings(),
    };

    [TestMethod]
    public void Apply_WithoutOutputName_UsesTheInputFileName()
    {
        var dto = ParsedDto();

        PrintMetadata.Apply(dto, InputFile, outputName: null, pluginVersion: "1.1.2.0");

        Assert.AreEqual("calibration_cube.gcode", dto.settings.file_name);
        Assert.AreEqual("Calibration Cube", dto.settings.print_name);
        Assert.AreEqual("1.1.2.0", dto.PluginVersion);
    }

    [TestMethod]
    public void Apply_WithOutputName_PrefersTheSlicersFinalFileName()
    {
        var dto = ParsedDto();
        string outputName = Path.Combine(Path.GetTempPath(), "Prints", "benchy_final.gcode");

        PrintMetadata.Apply(dto, InputFile, outputName, pluginVersion: "1.1.2.0");

        Assert.AreEqual("benchy_final.gcode", dto.settings.file_name);
        Assert.AreEqual("Benchy Final", dto.settings.print_name);
    }

    [TestMethod]
    public void Apply_WithBinaryGcode_KeepsTheBgcodeExtension()
    {
        var dto = ParsedDto();

        PrintMetadata.Apply(dto, Path.Combine(Path.GetTempPath(), "cube.bgcode"), outputName: null, pluginVersion: "1.1.2.0");

        Assert.AreEqual("cube.bgcode", dto.settings.file_name);
        Assert.AreEqual("Cube", dto.settings.print_name);
    }

    [TestMethod]
    public void BuildPrintUrl_ContainsVersionsAndSettingId()
    {
        var dto = ParsedDto();
        dto.PluginVersion = "1.1.2.0";

        string url = PrintMetadata.BuildPrintUrl("https://www.3dprintlog.com/prints/new/cura", dto, "abc-123");

        Assert.AreEqual("https://www.3dprintlog.com/prints/new/cura?cura_version=2.8.1&plugin_version=1.1.2.0&settingId=abc-123", url);
    }
}
