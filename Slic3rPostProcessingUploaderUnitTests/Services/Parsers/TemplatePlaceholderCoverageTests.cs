using Slic3rPostProcessingUploader.Services;
using Slic3rPostProcessingUploader.Services.Parsers;
using Slic3rPostProcessingUploader.Services.Parsers.AnycubicSlicerNext;
using Slic3rPostProcessingUploader.Services.Parsers.BambuStudio;
using Slic3rPostProcessingUploader.Services.Parsers.FLSunSlicer;
using Slic3rPostProcessingUploader.Services.Parsers.OrcaSlicer;
using Slic3rPostProcessingUploader.Services.Parsers.PrusaSlicer;

namespace Slic3rPostProcessingUploaderUnitTests.Services.Parsers;

/// <summary>
/// Guards the built-in note templates against slicer releases that rename or drop a setting. Without this, a renamed
/// key renders as a blank value and the snapshot test would happily record the blank as the new truth.
/// </summary>
[TestClass]
public sealed class TemplatePlaceholderCoverageTests
{
    private sealed record SlicerUnderTest(
        string Name,
        string NewestFixture,
        Func<string, IGcodeParser> CreateParser,
        INoteTemplate DefaultTemplate,
        INoteTemplate FullTemplate,
        IReadOnlyDictionary<string, string> KnownAbsent);

    /// <summary>
    /// Update <c>NewestFixture</c> when a newer G-code fixture is added under TestData/{Slicer}/.
    /// <c>KnownAbsent</c> lists placeholders that legitimately have no value in that fixture, with the reason why.
    /// </summary>
    private static readonly SlicerUnderTest[] Slicers =
    [
        new(
            "OrcaSlicer",
            Path.Combine("OrcaSlicer", "orcaslicer-2.2.0-rc-calibration-cube.gcode"),
            template => new OrcaParser(template),
            new OrcaDefaultNoteTemplate(),
            new OrcaFullNoteTemplate(),
            new Dictionary<string, string>()),
        new(
            "PrusaSlicer",
            Path.Combine("PrusaSlicer", "prusaslicer-3.0.0-alpha11-calibration-cube.gcode"),
            template => new PrusaParser(template),
            new PrusaDefaultNoteTemplate(),
            new PrusaFullNoteTemplate(),
            new Dictionary<string, string>()),
        new(
            "BambuStudio",
            Path.Combine("BambuStudio", "bambustudio-01.10.01.50-calibration-cube.gcode"),
            template => new BambuStudioParser(template),
            new BambuStudioDefaultNoteTemplate(),
            new BambuStudioFullNoteTemplate(),
            new Dictionary<string, string>()),
        new(
            "FLSunSlicer",
            Path.Combine("FLSunSlicer", "flsunslicer-2.0.2-calibration-cube.gcode"),
            template => new FLSunParser(template),
            new FLSunDefaultNoteTemplate(),
            new FLSunFullNoteTemplate(),
            new Dictionary<string, string>
            {
                ["infill_combination_max_layer_height"] = "FLSun Slicer 2.0.2 is based on an OrcaSlicer release that predates this setting",
            }),
        new(
            "AnycubicSlicerNext",
            Path.Combine("AnycubicSlicerNext", "anycubicslicernext-1.3.2-calibration-cube.gcode"),
            template => new AnycubicSlicerNextParser(template),
            new AnycubicSlicerNextDefaultNoteTemplate(),
            new AnycubicSlicerNextFullNoteTemplate(),
            new Dictionary<string, string>
            {
                ["ironing_type"] = "Anycubic Slicer Next 1.3.2 writes the ironing_* detail keys but not ironing_type",
            }),
    ];

    public static IEnumerable<object[]> Cases =>
        Slicers.SelectMany(slicer => new[] { "Default", "Full" }.Select(template => new object[] { slicer.Name, template }));

    [TestMethod]
    [DynamicData(nameof(Cases))]
    public void EveryTemplatePlaceholderResolvesAgainstNewestFixture(string slicerName, string templateName)
    {
        var slicer = Slicers.Single(s => s.Name == slicerName);
        var template = (templateName == "Default" ? slicer.DefaultTemplate : slicer.FullTemplate).getNoteTemplate();
        var gcode = TestData.Load(slicer.NewestFixture);

        var parser = (GcodeParserBase)slicer.CreateParser(template);
        var missing = parser.GetMissingPlaceholders(gcode);

        var unexpected = missing.Where(key => !slicer.KnownAbsent.ContainsKey(key)).ToList();
        Assert.IsTrue(
            unexpected.Count == 0,
            $"{unexpected.Count} placeholder(s) in the {slicerName} {templateName} template have no value in {Path.GetFileName(slicer.NewestFixture)}:\n  " +
            string.Join("\n  ", unexpected));

        // An allowlist entry that now resolves means the slicer started writing the setting again, so drop the entry
        // to restore coverage. Only placeholders this template actually uses can be judged.
        var stale = slicer.KnownAbsent.Keys
            .Where(key => template.Contains($"{{{{{key}}}}}", StringComparison.OrdinalIgnoreCase))
            .Except(missing, StringComparer.OrdinalIgnoreCase)
            .ToList();
        Assert.IsTrue(
            stale.Count == 0,
            $"KnownAbsent entries for {slicerName} now resolve in {Path.GetFileName(slicer.NewestFixture)} and should be removed:\n  " +
            string.Join("\n  ", stale));
    }
}
