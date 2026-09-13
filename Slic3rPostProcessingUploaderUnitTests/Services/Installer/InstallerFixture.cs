using System.Security.Cryptography;

namespace Slic3rPostProcessingUploaderUnitTests.Services.Installer;

/// <summary>
/// A scratch copy of one of the real slicer config trees under <c>TestData/Installer/{slicer}</c>.
/// The trees are pruned copies of genuine <c>%APPDATA%</c> folders (system process profiles plus every
/// user process profile) plus one fresh Linux install, so the installer is exercised against the layouts and
/// hand-made profiles it will actually meet. Disposing deletes the copy.
/// </summary>
internal sealed class InstallerFixture : IDisposable
{
    public const string OrcaSlicer = "OrcaSlicer";
    public const string SnapmakerOrca = "Snapmaker_Orca";
    public const string AnycubicSlicerNext = "AnycubicSlicerNext";
    /// <summary>OrcaSlicer 2.4.2 on Ubuntu 24.04 straight after the setup wizard (Custom + Afinia); no user profiles yet.</summary>
    public const string OrcaSlicerLinuxFresh = "OrcaSlicer-linux-fresh";

    public static readonly string[] All = [OrcaSlicer, SnapmakerOrca, AnycubicSlicerNext, OrcaSlicerLinuxFresh];

    /// <summary>The Windows trees, each carrying hand-made "… - 3D Print Log" profiles that already run the uploader.</summary>
    public static readonly string[] WithHandMadeProfiles = [OrcaSlicer, SnapmakerOrca, AnycubicSlicerNext];

    public string Slicer { get; }
    public string Root { get; }

    private InstallerFixture(string slicer, string root)
    {
        Slicer = slicer;
        Root = root;
    }

    public static InstallerFixture Copy(string slicer)
    {
        string source = TestData.FullPath(Path.Combine("Installer", slicer));
        string root = Path.Combine(Path.GetTempPath(), $"InstallerFixture_{slicer}_{Guid.NewGuid():N}");
        CopyDirectory(source, root);
        return new InstallerFixture(slicer, root);
    }

    public string UserProcessDir(string account = "default") =>
        Path.Combine(Root, "user", account, "process");

    public IEnumerable<string> AccountDirs() =>
        Directory.GetDirectories(Path.Combine(Root, "user")).Order(StringComparer.Ordinal);

    /// <summary>Relative path → SHA-256 of every file under the root; the basis for "nothing else changed" assertions.</summary>
    public IReadOnlyDictionary<string, string> Snapshot()
    {
        var result = new SortedDictionary<string, string>(StringComparer.Ordinal);
        foreach (string file in Directory.EnumerateFiles(Root, "*", SearchOption.AllDirectories))
        {
            string relative = Path.GetRelativePath(Root, file).Replace('\\', '/');
            result[relative] = Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(file)));
        }
        return result;
    }

    public void Dispose()
    {
        if (Directory.Exists(Root))
            Directory.Delete(Root, recursive: true);
    }

    private static void CopyDirectory(string source, string destination)
    {
        Directory.CreateDirectory(destination);
        foreach (string dir in Directory.EnumerateDirectories(source, "*", SearchOption.AllDirectories))
            Directory.CreateDirectory(Path.Combine(destination, Path.GetRelativePath(source, dir)));
        foreach (string file in Directory.EnumerateFiles(source, "*", SearchOption.AllDirectories))
            File.Copy(file, Path.Combine(destination, Path.GetRelativePath(source, file)));
    }
}
