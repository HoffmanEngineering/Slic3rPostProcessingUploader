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

        // The 4-part AssemblyVersion (e.g. "2.0.0.0") is the fallback; a real build must report the informational
        // version instead. Compared against the assembly's own version rather than a ".0.0" suffix so that a
        // legitimate X.0.0 release (built with -p:Version=2.0.0) does not trip the assertion.
        string assemblyVersion = typeof(VersionService).Assembly.GetName().Version!.ToString();
        Assert.AreNotEqual(assemblyVersion, version, $"Expected the informational version, got the padded AssemblyVersion '{version}'");
        Assert.AreEqual(3, version.Split('-')[0].Split('.').Length, $"Expected a three-part version, got '{version}'");
    }
}
