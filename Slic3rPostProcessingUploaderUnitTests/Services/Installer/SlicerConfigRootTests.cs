using Slic3rPostProcessingUploader.Services.Installer;
using System.Runtime.InteropServices;

namespace Slic3rPostProcessingUploaderUnitTests.Services.Installer;

[TestClass]
public class SlicerConfigRootTests
{
    [TestMethod]
    public void Windows_UsesRoamingAppData()
    {
        var root = SlicerConfigRoot.Resolve(OSPlatform.Windows, "OrcaSlicer", appData: @"C:\Users\me\AppData\Roaming", home: @"C:\Users\me", xdgConfigHome: null);
        Assert.AreEqual(@"C:\Users\me\AppData\Roaming\OrcaSlicer", root);
    }

    [TestMethod]
    public void MacOS_UsesLibraryApplicationSupportUnderHome()
    {
        var root = SlicerConfigRoot.Resolve(OSPlatform.OSX, "OrcaSlicer", appData: null, home: "/Users/me", xdgConfigHome: null);
        Assert.AreEqual("/Users/me/Library/Application Support/OrcaSlicer", root);
    }

    [TestMethod]
    public void Linux_UsesDotConfigUnderHome()
    {
        var root = SlicerConfigRoot.Resolve(OSPlatform.Linux, "OrcaSlicer", appData: null, home: "/home/me", xdgConfigHome: null);
        Assert.AreEqual("/home/me/.config/OrcaSlicer", root);
    }

    [TestMethod]
    public void Linux_PrefersXdgConfigHome()
    {
        var root = SlicerConfigRoot.Resolve(OSPlatform.Linux, "OrcaSlicer", appData: null, home: "/home/me", xdgConfigHome: "/mnt/cfg");
        Assert.AreEqual("/mnt/cfg/OrcaSlicer", root);
    }

    [TestMethod]
    public void Linux_IgnoresBlankXdgConfigHome()
    {
        var root = SlicerConfigRoot.Resolve(OSPlatform.Linux, "OrcaSlicer", appData: null, home: "/home/me", xdgConfigHome: "  ");
        Assert.AreEqual("/home/me/.config/OrcaSlicer", root);
    }

    [TestMethod]
    public void UnknownHome_YieldsNoRootRatherThanARelativePath()
    {
        // A relative ".config/OrcaSlicer" would silently resolve against the current directory.
        Assert.IsNull(SlicerConfigRoot.Resolve(OSPlatform.Linux, "OrcaSlicer", appData: null, home: "", xdgConfigHome: null));
        Assert.IsNull(SlicerConfigRoot.Resolve(OSPlatform.OSX, "OrcaSlicer", appData: null, home: null, xdgConfigHome: null));
        Assert.IsNull(SlicerConfigRoot.Resolve(OSPlatform.Windows, "OrcaSlicer", appData: "", home: @"C:\Users\me", xdgConfigHome: null));
    }

    [TestMethod]
    public void ForCurrentMachine_IsAbsolute()
    {
        var root = SlicerConfigRoot.ForCurrentMachine("OrcaSlicer");
        Assert.IsNotNull(root);
        Assert.IsTrue(Path.IsPathRooted(root), root);
    }
}
