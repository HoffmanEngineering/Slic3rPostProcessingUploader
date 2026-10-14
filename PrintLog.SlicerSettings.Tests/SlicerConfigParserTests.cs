using System.Diagnostics;
using System.Text;
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
        new[] { "OrcaSlicer", "AnycubicSlicerNext", "BambuStudio", "PrusaSlicer", "FLSunSlicer" }
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
        Assert.AreEqual(full.settings.estimated_print_time_seconds, result.EstimatedPrintTimeSeconds ?? 0);
        if (full.settings.material_used_mg is { } mg)
        {
            Assert.AreEqual(mg, result.TotalFilamentMg ?? 0);
        }

        // The uploader's per-slot rows are a projection of the result's slots.
        foreach (var (row, slot) in (full.settings.filamentUsage ?? []).Zip(result.Filaments))
        {
            Assert.AreEqual(row.EstimatedLengthInM, Math.Round(slot.LengthMm / 1000, 3));
            Assert.AreEqual(row.Notes, slot.Description);
        }

        Assert.AreEqual(full.settings.filamentUsage?.Count ?? result.Filaments.Count, result.Filaments.Count);
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
    public void Parse_OrcaTwoFilament_ReportsEachSlotAndTheTotals()
    {
        var gcode = TestData.Load(Path.Combine("OrcaSlicer", "orcaslicer-2.2.0-rc-calibration-cube-two-filament.gcode"));
        var (_, configBlock) = ConfigBlockFixture.Split(gcode);

        var result = SlicerConfigParser.Parse(configBlock);

        // ; filament used [mm] = 1519.88, 1874.36  /  ; filament used [g] = 4.53, 5.59
        Assert.AreEqual(2, result.Filaments.Count);
        Assert.AreEqual(0, result.Filaments[0].SlotIndex);
        Assert.AreEqual(1519.88, result.Filaments[0].LengthMm);
        Assert.AreEqual(1, result.Filaments[1].SlotIndex);
        Assert.AreEqual(1874.36, result.Filaments[1].LengthMm);
        StringAssert.StartsWith(result.Filaments[0].Description, "Slot 1");
        Assert.AreEqual(3394.24, result.TotalFilamentLengthMm!.Value, 0.001);
        Assert.AreEqual(10120, result.TotalFilamentMg);
        Assert.AreEqual(42 * 60 + 33, result.EstimatedPrintTimeSeconds);
    }

    [TestMethod]
    public void Parse_BambuConfigBlockWithoutHeader_HasNoTotals()
    {
        // Bambu Studio writes its time and filament totals in the header, not in the settings block, so without the
        // header they are absent: null, not a zero that reads as "this print uses nothing".
        var gcode = TestData.Load(Path.Combine("BambuStudio", "bambustudio-01.10.01.50-calibration-cube-two-filament.gcode"));
        var (header, configBlock) = ConfigBlockFixture.Split(gcode);

        var withoutHeader = SlicerConfigParser.Parse(configBlock);
        var withHeader = SlicerConfigParser.Parse(configBlock, header);

        Assert.IsNull(withoutHeader.EstimatedPrintTimeSeconds);
        Assert.IsNull(withoutHeader.TotalFilamentMg);
        Assert.IsNull(withoutHeader.TotalFilamentLengthMm);
        Assert.AreEqual(0, withoutHeader.Filaments.Count);

        Assert.AreEqual(4 * 3600 + 12 * 60 + 53, withHeader.EstimatedPrintTimeSeconds);
        Assert.AreEqual(62130, withHeader.TotalFilamentMg);
        Assert.AreEqual(2, withHeader.Filaments.Count);
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

        var result = SlicerConfigParser.Parse(configBlock, "; generated by OrcaSlicer 2.4.0 on 2026-01-01 at 10:00:00");

        StringAssert.Contains(result.Note, "Profile Changes:\n  Changed from \"0.20mm Standard @MyPrinter\": sparse_infill_density = 8%\n");
    }

    [TestMethod]
    [DataRow("")]
    [DataRow("; nothing to see here\n")]
    [DataRow("G1 X10 Y10\n; layer_height = \n")]
    // Thumbnail dimensions that do not fit an int.
    [DataRow("; thumbnail begin 99999999999x1 5\n; x\n; thumbnail end\n")]
    [DataRow("; thumbnail begin 2147483647x2147483647 5\n; x\n; thumbnail end\n; thumbnail begin 2x2 5\n; y\n; thumbnail end\n")]
    // Durations and weights far outside an int once converted to seconds or milligrams.
    [DataRow("; estimated printing time (normal mode) = 99999999d\n")]
    [DataRow("; estimated printing time (normal mode) = -5m\n")]
    [DataRow("; filament used [g] = 1e300\n")]
    [DataRow("; filament used [g] = 3000000\n")]
    [DataRow("; filament used [mm] = Infinity, NaN, 1e400\n; filament_diameter = 1.75\n; filament_density = 1.24\n")]
    [DataRow("; filament used [mm] = 1e300\n; filament_diameter = 1.75\n; filament_density = 1.24\n")]
    public void Parse_MalformedText_DoesNotThrow(string configBlock)
    {
        var result = SlicerConfigParser.Parse(configBlock);

        Assert.IsFalse(result.IsRecognized);
        Assert.AreEqual("Unknown", result.Version);
        Assert.IsTrue(result.EstimatedPrintTimeSeconds is null or > 0, $"time {result.EstimatedPrintTimeSeconds}");
        Assert.IsTrue(result.TotalFilamentMg is null or > 0, $"mg {result.TotalFilamentMg}");
        Assert.IsTrue(result.TotalFilamentLengthMm is null or > 0 && double.IsFinite(result.TotalFilamentLengthMm ?? 1), $"mm {result.TotalFilamentLengthMm}");
        Assert.IsTrue(result.Filaments.All(f => double.IsFinite(f.LengthMm) && f.LengthMm > 0));
    }

    [TestMethod]
    [DataRow("")]
    [DataRow("; nothing to see here\n")]
    [DataRow("G1 X10 Y10\n; layer_height = \n")]
    public void Parse_TextWithoutSettings_FallsBackToOrcaWithNothingFound(string configBlock)
    {
        var result = SlicerConfigParser.Parse(configBlock);

        Assert.AreEqual(SlicerNameOf(typeof(OrcaParser)), result.Slicer);
        Assert.AreEqual(0, result.Settings.Count);
        Assert.IsNull(result.EstimatedPrintTimeSeconds);
        Assert.IsNull(result.TotalFilamentMg);
        Assert.IsNull(result.TotalFilamentLengthMm);
        Assert.AreEqual(0, result.Filaments.Count);
        StringAssert.StartsWith(result.Note, "Profiles:");
    }

    [TestMethod]
    public void Parse_ThrowingDebugLog_IsSwallowed()
    {
        // {{models}} has no toolpath to measure, so it reports through the debug log. A caller's log that throws must
        // not turn a parse into an exception.
        var configBlock = "; print_settings_id = P\n; layer_height = 0.2\n";

        var result = SlicerConfigParser.Parse(configBlock, "; generated by OrcaSlicer 2.4.0 on 2026-01-01", _ => throw new InvalidOperationException("log is down"));

        Assert.AreEqual("0.2", result.Settings["layer_height"]);
    }

    [TestMethod]
    public void Parse_NullConfigBlock_Throws()
    {
        Assert.ThrowsException<ArgumentNullException>(() => SlicerConfigParser.Parse(null!));
    }

    // ~1 MB of hostile input each. Before the fix the first took 43 s: every unterminated "thumbnail begin" rescanned
    // to the end of the text. A generous bound still catches anything quadratic at this size.
    [TestMethod]
    [Timeout(30_000)]
    [DataRow("; thumbnail begin 1x1 5\n", "unterminated thumbnails")]
    [DataRow("; thumbnail_JPG begin 1x1 5\n", "unterminated JPG thumbnails")]
    [DataRow("; generated by OrcaSlicer x ", "a version marker with no ' on', on one line")]
    [DataRow("; generated by PrusaSlicer x\n", "version markers with no ' on'")]
    [DataRow("; total estimated time: 1", "a time with no line end")]
    [DataRow("; BambuStudio ", "a Bambu marker with no whitespace after the version")]
    [DataRow("; a = b = c = d = e = f = g = h ", "one long setting line full of separators")]
    public void Parse_HostileInput_IsLinear(string repeated, string description)
    {
        var text = new StringBuilder().Insert(0, repeated, 1_000_000 / repeated.Length).ToString();

        SlicerConfigParser.Parse(text);
        var stopwatch = Stopwatch.StartNew();
        SlicerConfigParser.Parse(text, text);
        stopwatch.Stop();

        Assert.IsTrue(stopwatch.Elapsed < TimeSpan.FromSeconds(1), $"{description}: {stopwatch.ElapsedMilliseconds} ms");
    }

    [TestMethod]
    public void PublicSurface_IsOnlyTheEntryPointAndItsResult()
    {
        // nuget.org versions are immutable: anything public here is a promise to the API. Widen it deliberately.
        var exported = typeof(SlicerConfigParser).Assembly.GetExportedTypes().Select(t => t.FullName).Order(StringComparer.Ordinal).ToList();

        CollectionAssert.AreEqual(
            new[]
            {
                "PrintLog.SlicerSettings.FilamentEstimate",
                "PrintLog.SlicerSettings.SlicerConfigParser",
                "PrintLog.SlicerSettings.SlicerConfigResult",
            },
            exported,
            string.Join(", ", exported));
    }

    [TestMethod]
    [DynamicData(nameof(Fixtures))]
    public void Split_TakesTheSettingsBlockAndNoToolpath(string fixturePath)
    {
        var lines = TestData.Load(fixturePath).ReplaceLineEndings("\n").Split('\n');
        var (start, end) = ConfigBlockFixture.Range(lines);

        // The toolpath, found independently of Split: between the executable markers where the slicer writes them;
        // for PrusaSlicer, whose settings are one line each, from the first to the last uncommented line. The trimmed
        // Bambu and FLSun fixtures keep no toolpath at all (their settings block holds multi-line custom G-code, so
        // uncommented lines there are not moves).
        var executableStart = Array.IndexOf(lines, "; EXECUTABLE_BLOCK_START");
        var executableEnd = Array.IndexOf(lines, "; EXECUTABLE_BLOCK_END");
        (int Start, int End)? toolpath =
            executableStart >= 0 && executableEnd >= 0 ? (executableStart + 1, executableEnd - 1)
            : !lines.Contains("; CONFIG_BLOCK_START") ? (Array.FindIndex(lines, IsCommand), Array.FindLastIndex(lines, IsCommand))
            : null;

        if (toolpath is var (toolpathStart, toolpathEnd))
        {
            Assert.IsTrue(toolpathStart >= 0 && toolpathEnd > toolpathStart + 10, $"{fixturePath}: no toolpath found");
            Assert.IsTrue(end < toolpathStart || start > toolpathEnd, $"{fixturePath}: block {start}-{end} overlaps toolpath {toolpathStart}-{toolpathEnd}");
        }

        var block = lines[start..(end + 1)];
        Assert.IsTrue(
            // Anycubic Slicer Next writes "; CONFIG_BLOCK_END = end".
            block.Any(line => line.StartsWith("; CONFIG_BLOCK_END", StringComparison.Ordinal)) || block.Contains("; prusaslicer_config = end"),
            $"{fixturePath}: the block should run to the end of the settings");

        static bool IsCommand(string line) => line.Length > 0 && !line.TrimStart().StartsWith(';');
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

        var (start, end) = Range(lines);
        return (header, string.Join("\n", lines[start..(end + 1)]));
    }

    /// <summary>First and last line (inclusive) of the settings block.</summary>
    public static (int Start, int End) Range(string[] lines)
    {
        // Bambu Studio and FLSun Slicer write the settings block near the top rather than at the end of the file:
        // everything after the header through CONFIG_BLOCK_END (FLSun puts its time and filament totals just before
        // CONFIG_BLOCK_START).
        var configStart = Array.FindIndex(lines, line => line == "; CONFIG_BLOCK_START");
        var executableStart = Array.FindIndex(lines, line => line == "; EXECUTABLE_BLOCK_START");
        if (configStart >= 0 && (executableStart < 0 || configStart < executableStart))
        {
            var headerEnd = Array.FindIndex(lines, line => line.Trim() == "; HEADER_BLOCK_END");
            return (headerEnd + 1, Array.FindIndex(lines, configStart, line => line == "; CONFIG_BLOCK_END"));
        }

        // Everyone else writes it after the toolpath: from EXECUTABLE_BLOCK_END where the slicer writes one (comments
        // can follow the last command inside that block), otherwise every comment line after the last command.
        var executableEnd = Array.IndexOf(lines, "; EXECUTABLE_BLOCK_END");
        var lastToolpath = Array.FindLastIndex(lines, line => line.Length > 0 && !line.StartsWith(';'));
        return (executableEnd >= 0 ? executableEnd : lastToolpath + 1, lines.Length - 1);
    }
}
