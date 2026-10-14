namespace Slic3rPostProcessingUploaderUnitTests;

/// <summary>
/// Fixtures for the uploader's own tests (the installer's slicer config trees). G-code fixtures live with the parsers
/// in PrintLog.SlicerSettings.Tests.
/// </summary>
internal static class TestData
{
    private static readonly string Root = Path.Combine(AppContext.BaseDirectory, "TestData");

    public static string FullPath(string relativePath) =>
        Path.Combine(Root, relativePath);
}
