using System.Text.Json;
using PrintLog.SlicerSettings.AnycubicSlicerNext;
using PrintLog.SlicerSettings.BambuStudio;
using PrintLog.SlicerSettings.OrcaSlicer;
using PrintLog.SlicerSettings.PrusaSlicer;

namespace PrintLog.SlicerSettings.Tests;

/// <summary>
/// The entry point a connector-facing API uses: it has the settings block a slicer writes (and maybe the header), never
/// the toolpaths. Each case cuts that text out of a real fixture and compares the result with parsing the whole file.
/// </summary>
[TestClass]
public sealed class SlicerConfigParserTests
{
    public static IEnumerable<object[]> Fixtures =>
        new[] { "OrcaSlicer", "AnycubicSlicerNext", "BambuStudio", "PrusaSlicer" }
            .SelectMany(TestData.EnumerateFixtures)
            // Binary G-code decodes to a synthetic header with no settings block to cut out.
            .Where(path => path.EndsWith(".gcode", StringComparison.OrdinalIgnoreCase))
            .Select(path => new object[] { path });

    [TestMethod]
    [DynamicData(nameof(Fixtures))]
    public void Parse_ConfigBlockWithHeader_MatchesParsingTheWholeFile(string fixturePath)
    {
        var gcode = TestData.Load(fixturePath);
        var (header, configBlock) = ConfigBlockFixture.Split(gcode);

        var result = SlicerConfigParser.Parse(configBlock, header);

        var full = ParseWholeFileWithoutModels(gcode);
        Assert.IsTrue(result.IsRecognized);
        Assert.AreEqual(full.Slicer, result.Slicer);
        Assert.AreEqual(full.CuraVersion, result.Version);
        Assert.AreNotEqual("Unknown", result.Version);
        Assert.AreEqual(full.settings.note, result.Note);
        Assert.AreEqual(full.settings.note, result.Dto.settings.note);
        Assert.AreEqual(full.settings.estimated_print_time_seconds, result.Dto.settings.estimated_print_time_seconds);
        Assert.AreEqual(full.settings.material_used_mg, result.Dto.settings.material_used_mg);
        Assert.AreEqual(Json(full.settings.filamentUsage), Json(result.Dto.settings.filamentUsage));
    }

    [TestMethod]
    [DynamicData(nameof(Fixtures))]
    public void Parse_ConfigBlockOnly_ReturnsTheSettingsAndARenderedNote(string fixturePath)
    {
        var gcode = TestData.Load(fixturePath);
        var (_, configBlock) = ConfigBlockFixture.Split(gcode);

        var result = SlicerConfigParser.Parse(configBlock);

        var layerHeight = Setting(gcode, "layer_height");
        Assert.AreEqual(layerHeight, result.Settings["layer_height"]);
        Assert.AreEqual(layerHeight, result.Settings["LAYER_HEIGHT"], "lookups ignore case, like the template's");
        // Profile names; PrusaSlicer 3.0 alpha fixtures were exported without named presets, so theirs are empty.
        Assert.AreEqual(Setting(gcode, "print_settings_id"), result.Settings.GetValueOrDefault("print_settings_id", string.Empty));
        Assert.AreEqual(Setting(gcode, "printer_settings_id"), result.Settings.GetValueOrDefault("printer_settings_id", string.Empty));
        StringAssert.Contains(result.Note, $"Layer Height: {layerHeight}");
        Assert.AreEqual("Unknown", result.Version, "the version is only in the header");
    }

    [TestMethod]
    [DataRow("OrcaSlicer/orcaslicer-2.4.0-benchy-x16.gcode", typeof(OrcaParser))]
    [DataRow("PrusaSlicer/prusaslicer-2.9.2-calibration-cube.gcode", typeof(PrusaParser))]
    [DataRow("BambuStudio/bambustudio-01.10.01.50-calibration-cube.gcode", typeof(BambuStudioParser))]
    public void Parse_ConfigBlockOnly_GuessesTheSlicerFromItsSettings(string fixturePath, Type expectedParser)
    {
        var gcode = TestData.Load(fixturePath);
        var (_, configBlock) = ConfigBlockFixture.Split(gcode);

        var result = SlicerConfigParser.Parse(configBlock);

        Assert.IsFalse(result.IsRecognized, "no header, so no slicer marker: the slicer is a best guess");
        Assert.AreEqual(SlicerNameOf(expectedParser), result.Slicer);
    }

    [TestMethod]
    public void Parse_AnycubicConfigBlockOnly_IsReadAsTheOrcaFamily()
    {
        // Anycubic Slicer Next is an OrcaSlicer fork and its settings block carries nothing of its own, so without the
        // header it cannot be told apart from Orca. Tie-breaking picks Orca, whose note is a superset of Anycubic's.
        var gcode = TestData.Load(Path.Combine("AnycubicSlicerNext", "anycubicslicernext-1.3.2-calibration-cube.gcode"));
        var (_, configBlock) = ConfigBlockFixture.Split(gcode);

        var result = SlicerConfigParser.Parse(configBlock);

        Assert.AreEqual(SlicerNameOf(typeof(OrcaParser)), result.Slicer);
        StringAssert.Contains(result.Note, $"Print: {Setting(gcode, "print_settings_id")}");
    }

    [TestMethod]
    public void Parse_WithHeader_UsesTheHeadersSlicerMarker()
    {
        var gcode = TestData.Load(Path.Combine("AnycubicSlicerNext", "anycubicslicernext-1.3.2-calibration-cube.gcode"));
        var (header, configBlock) = ConfigBlockFixture.Split(gcode);

        var result = SlicerConfigParser.Parse(configBlock, header);

        Assert.AreEqual(SlicerNameOf(typeof(AnycubicSlicerNextParser)), result.Slicer);
        Assert.AreEqual("1.3.2", result.Version);
    }

    [TestMethod]
    public void Parse_OrcaConfigBlock_RendersProfilesAndModifiedSettingsButNoModels()
    {
        var gcode = TestData.Load(Path.Combine("OrcaSlicer", "orcaslicer-2.4.0-benchy-x16.gcode"));
        var (header, configBlock) = ConfigBlockFixture.Split(gcode);

        var result = SlicerConfigParser.Parse(configBlock, header);

        StringAssert.StartsWith(result.Note, "Profiles:\n  Print: 0.10mm FAST DETAIL @MK4S 0.4 - 3DPrintLog\n");
        StringAssert.Contains(result.Note, "Filament: Prusa Generic PLA @MK4S");
        // {{models}} measures objects from the toolpaths, which a settings block does not have.
        Assert.IsFalse(result.Note.Contains("Models:", StringComparison.Ordinal), result.Note);
    }

    [TestMethod]
    public void Parse_ModifiedSettings_IsTheFlatListBecauseNoPresetFilesAreConsulted()
    {
        const string configBlock = """
            ; CONFIG_BLOCK_START
            ; different_settings_to_system = sparse_infill_density;;
            ; inherits_group = "0.20mm Standard @MyPrinter";;
            ; print_settings_id = My Process
            ; sparse_infill_density = 8%
            ; CONFIG_BLOCK_END
            """;
        var debug = new List<string>();

        var result = SlicerConfigParser.Parse(configBlock, "; generated by OrcaSlicer 2.4.0 on 2026-01-01 at 10:00:00", debug.Add);

        StringAssert.Contains(result.Note, "Profile Changes:\n  Changed from \"0.20mm Standard @MyPrinter\": sparse_infill_density = 8%\n");
    }

    [TestMethod]
    [DataRow("")]
    [DataRow("; nothing to see here\n")]
    [DataRow("G1 X10 Y10\n; layer_height = \n")]
    public void Parse_TextWithoutSettings_DoesNotThrow(string configBlock)
    {
        var result = SlicerConfigParser.Parse(configBlock);

        Assert.AreEqual(SlicerNameOf(typeof(OrcaParser)), result.Slicer);
        Assert.AreEqual("Unknown", result.Version);
        Assert.IsFalse(result.IsRecognized);
        Assert.AreEqual(0, result.Settings.Count);
        StringAssert.StartsWith(result.Note, "Profiles:");
    }

    [TestMethod]
    public void Parse_NullConfigBlock_Throws()
    {
        Assert.ThrowsException<ArgumentNullException>(() => SlicerConfigParser.Parse(null!));
    }

    [TestMethod]
    public void Split_LeavesTheToolpathsOut()
    {
        foreach (var fixturePath in Fixtures.Select(f => (string)f[0]))
        {
            var (_, configBlock) = ConfigBlockFixture.Split(TestData.Load(fixturePath));

            // Custom G-code settings span lines, so a settings block legitimately holds a few uncommented commands.
            var moves = configBlock.Split('\n').Count(line => line.StartsWith("G1 ", StringComparison.Ordinal));
            Assert.IsTrue(moves < 20, $"{fixturePath}: {moves} G1 moves is a toolpath, not a settings block");
            Assert.IsFalse(configBlock.Contains("; EXECUTABLE_BLOCK_START", StringComparison.Ordinal), fixturePath);
        }
    }

    /// <summary>
    /// The whole-file parse with the default template, less <c>{{models}}</c>: a settings block has no toolpaths, so
    /// that section is the one thing the config-block parse cannot reproduce.
    /// </summary>
    private static CuraSettingDto ParseWholeFileWithoutModels(string gcode)
    {
        var slicer = SlicerDetector.Detect(gcode);
        var template = slicer.DefaultTemplate.getNoteTemplate().Replace("{{models}}\n", string.Empty, StringComparison.Ordinal);
        return slicer.CreateParser(template).ParseGcode(gcode);
    }

    /// <summary>The first value the file gives the key, or empty; read independently of the parser.</summary>
    private static string Setting(string gcode, string key)
    {
        var prefix = $"; {key} =";
        var line = gcode.Split('\n').Select(l => l.TrimEnd('\r')).First(l => l.StartsWith(prefix, StringComparison.Ordinal));
        return line[prefix.Length..].Trim();
    }

    private static string SlicerNameOf(Type parserType) =>
        ((IGcodeParser)Activator.CreateInstance(parserType, [null])!).ParseGcode(string.Empty).Slicer;

    private static string Json(object? value) => JsonSerializer.Serialize(value);
}

/// <summary>
/// Cuts a fixture into what a connector would send: the header and the settings block, without toolpaths.
/// </summary>
internal static class ConfigBlockFixture
{
    public static (string Header, string ConfigBlock) Split(string gcode)
    {
        var lines = gcode.ReplaceLineEndings("\n").Split('\n');

        // Orca-family and Bambu files open with a header block; PrusaSlicer's header is its first line.
        var headerEnd = Array.FindIndex(lines, line => line.Trim() == "; HEADER_BLOCK_END");
        var header = string.Join("\n", lines[..(headerEnd >= 0 ? headerEnd + 1 : 1)]);

        // Bambu Studio writes its settings block right after the header rather than at the end of the file.
        var configStart = Array.FindIndex(lines, line => line == "; CONFIG_BLOCK_START");
        var executableStart = Array.FindIndex(lines, line => line == "; EXECUTABLE_BLOCK_START");
        if (configStart >= 0 && (executableStart < 0 || configStart < executableStart))
        {
            var configEnd = Array.FindIndex(lines, configStart, line => line == "; CONFIG_BLOCK_END");
            return (header, string.Join("\n", lines[configStart..(configEnd + 1)]));
        }

        // Everyone else: every comment line after the last toolpath command.
        var lastToolpath = Array.FindLastIndex(lines, line => line.Length > 0 && !line.StartsWith(';'));
        return (header, string.Join("\n", lines[(lastToolpath + 1)..]));
    }
}
