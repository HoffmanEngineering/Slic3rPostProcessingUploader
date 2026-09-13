using Slic3rPostProcessingUploader.Services.Parsers;
using Slic3rPostProcessingUploader.Services.Parsers.OrcaSlicer;
using Snapshooter.MSTest;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace Slic3rPostProcessingUploaderUnitTests.Services.Parsers;

/// <summary>
/// Pins the exact JSON the API receives. The parser snapshots use Snapshooter's own serializer, so they would not
/// notice a change to <see cref="CuraSettingDto.ToJSON"/> (property casing, indentation, dropped fields).
/// </summary>
[TestClass]
public sealed class CuraSettingDtoJsonTests
{
    [TestMethod]
    public void ToJSON_UsesCamelCasePropertyNames()
    {
        var dto = new CuraSettingDto
        {
            Slicer = "OrcaSlicer",
            CuraVersion = "2.2.0",
            PluginVersion = "1.1.2.0",
            settings = new CuraSettings { Snapshot = "AAAA", file_name = "cube.gcode" },
        };

        var json = JsonNode.Parse(dto.ToJSON())!.AsObject();

        Assert.AreEqual("OrcaSlicer", (string?)json["slicer"]);
        Assert.AreEqual("2.2.0", (string?)json["curaVersion"]);
        Assert.AreEqual("1.1.2.0", (string?)json["pluginVersion"]);
        Assert.AreEqual("AAAA", (string?)json["settings"]?["snapshot"]);
        Assert.AreEqual("cube.gcode", (string?)json["settings"]?["file_name"]);
        Assert.IsNull(json["Slicer"]);
    }

    [TestMethod]
    public void ToJSON_ForAParsedFixture_MatchesTheWireFormatSnapshot()
    {
        var gcode = TestData.Load(Path.Combine("OrcaSlicer", "orcaslicer-2.2.0-rc-calibration-cube.gcode"));
        var dto = new OrcaParser(new OrcaDefaultNoteTemplate().getNoteTemplate()).ParseGcode(gcode);
        dto.PluginVersion = "1.1.2.0";
        dto.settings.file_name = "orcaslicer-2.2.0-rc-calibration-cube.gcode";
        dto.settings.print_name = "Calibration Cube";

        Snapshot.Match(HashSnapshotField(dto.ToJSON()));
    }

    // Mirrors Snapshooter's HashField: the base64 thumbnail is large and platform-independent, so its SHA-256 is enough.
    // The note's line endings follow the template source file's checkout (CRLF on Windows, LF elsewhere), so they are
    // normalized the same way Snapshooter does for the parser snapshots.
    private static string HashSnapshotField(string json)
    {
        var root = JsonNode.Parse(json)!.AsObject();
        var settings = root["settings"]!.AsObject();
        string? snapshot = (string?)settings["snapshot"];
        if (snapshot != null)
        {
            settings["snapshot"] = Convert.ToBase64String(SHA256.HashData(Encoding.UTF8.GetBytes(snapshot)));
        }

        string? note = (string?)settings["note"];
        if (note != null)
        {
            settings["note"] = note.ReplaceLineEndings("\n");
        }

        return root.ToJsonString(new JsonSerializerOptions { WriteIndented = true, NewLine = "\n" });
    }
}
