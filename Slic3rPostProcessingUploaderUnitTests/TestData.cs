namespace Slic3rPostProcessingUploaderUnitTests;

internal static class TestData
{
    private static readonly string Root = Path.Combine(AppContext.BaseDirectory, "TestData");

    public static string Load(string relativePath) =>
        File.ReadAllText(Path.Combine(Root, relativePath));

    public static IEnumerable<string> EnumerateFixtures(string slicerFolder) =>
        Directory.EnumerateFiles(Path.Combine(Root, slicerFolder), "*.gcode")
            .Select(path => Path.GetRelativePath(Root, path))
            .Order(StringComparer.Ordinal);
}
