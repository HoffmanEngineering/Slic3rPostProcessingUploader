using Slic3rPostProcessingUploader.Services.Parsers;

namespace Slic3rPostProcessingUploaderUnitTests;

internal static class TestData
{
    private static readonly string Root = Path.Combine(AppContext.BaseDirectory, "TestData");

    public static string FullPath(string relativePath) =>
        Path.Combine(Root, relativePath);

    /// <summary>
    /// Loads a fixture as the text the parsers see: ASCII G-code as-is, binary G-code decoded to its synthetic header.
    /// </summary>
    public static string Load(string relativePath) =>
        BinaryGcode.IsBinaryGcodeFile(FullPath(relativePath))
            ? BinaryGcode.ReadFromFile(FullPath(relativePath))
            : File.ReadAllText(FullPath(relativePath));

    /// <summary>
    /// Every .gcode and .bgcode fixture in a slicer's folder.
    /// </summary>
    public static IEnumerable<string> EnumerateFixtures(string slicerFolder) =>
        Directory.EnumerateFiles(FullPath(slicerFolder))
            .Where(path => path.EndsWith(".gcode", StringComparison.OrdinalIgnoreCase) || path.EndsWith(".bgcode", StringComparison.OrdinalIgnoreCase))
            .Select(path => Path.GetRelativePath(Root, path))
            .Order(StringComparer.Ordinal);
}
