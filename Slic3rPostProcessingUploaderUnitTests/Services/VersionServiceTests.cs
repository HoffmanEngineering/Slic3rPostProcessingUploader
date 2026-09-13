using Slic3rPostProcessingUploader.Services;

namespace Slic3rPostProcessingUploaderUnitTests.Services;

[TestClass]
public sealed class VersionServiceTests
{
    [TestMethod]
    public void Format_UsesTheInformationalVersionAsWritten()
    {
        Assert.AreEqual("1.2.0", VersionService.Format("1.2.0", new Version(1, 2, 0, 0)));
    }

    [TestMethod]
    public void Format_KeepsPrereleaseSuffixes()
    {
        Assert.AreEqual("1.2.0-beta.1", VersionService.Format("1.2.0-beta.1", new Version(1, 2, 0, 0)));
    }

    [TestMethod]
    public void Format_StripsTheSourceRevisionSuffix()
    {
        Assert.AreEqual("1.2.0", VersionService.Format("1.2.0+3f1a2b4c", new Version(1, 2, 0, 0)));
    }

    [TestMethod]
    public void Format_FallsBackToTheAssemblyVersion()
    {
        Assert.AreEqual("1.2.0.0", VersionService.Format(null, new Version(1, 2, 0, 0)));
        Assert.AreEqual("1.2.0.0", VersionService.Format("   ", new Version(1, 2, 0, 0)));
    }

    [TestMethod]
    public void Format_ReportsUnknownWhenNothingIsAvailable()
    {
        Assert.AreEqual("Unknown", VersionService.Format(null, null));
    }

    [TestMethod]
    public void GetVersion_ReadsTheBuiltAssembly()
    {
        // The csproj's <Version> is a global-property override point for release builds; whatever it
        // resolves to, the runtime must report it without the 4-part ".0" padding of AssemblyVersion.
        string version = new VersionService().GetVersion();

        Assert.AreNotEqual("Unknown", version);
        Assert.IsFalse(version.EndsWith(".0.0", StringComparison.Ordinal), $"Expected the informational version, got '{version}'");
    }
}
