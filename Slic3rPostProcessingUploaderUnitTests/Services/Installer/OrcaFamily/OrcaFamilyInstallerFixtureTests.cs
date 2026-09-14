using Slic3rPostProcessingUploader.Services.Installer.OrcaFamily;
using Snapshooter.MSTest;
using System.Text.Json.Nodes;

namespace Slic3rPostProcessingUploaderUnitTests.Services.Installer.OrcaFamily;

/// <summary>
/// Runs the OrcaSlicer-family installer against pruned copies of real <c>%APPDATA%</c> config trees
/// (see <see cref="InstallerFixture"/>). Each tree also carries the hand-made "… - 3D Print Log" profiles
/// a user who configured the uploader manually would already have, so these tests pin down how the
/// installer treats profiles it did not create.
/// </summary>
[TestClass]
public class OrcaFamilyInstallerFixtureTests
{
    private const string ExePath = @"C:\Program Files\3D Print Log\Slic3rPostProcessingUploader.exe";
    private const string Flags = "--full";
    private const string ExpectedEntry = "\"" + ExePath + "\" " + Flags;
    private const string Suffix = " - 3DPrintLog";

    private sealed class FixtureInstaller(InstallerFixture fixture) : OrcaFamilyProfileInstaller(fixture.Root)
    {
        public override string SlicerName => fixture.Slicer;
        protected override string SlicerDirectoryName => fixture.Slicer;
    }

    private static IEnumerable<object[]> Slicers => InstallerFixture.All.Select(s => new object[] { s });
    private static IEnumerable<object[]> SlicersWithHandMadeProfiles => InstallerFixture.WithHandMadeProfiles.Select(s => new object[] { s });

    /// <summary>One hand-made profile per fixture: (slicer, the system profile it inherits, its file name).</summary>
    private static IEnumerable<object[]> HandMadeProfiles =>
    [
        [InstallerFixture.OrcaSlicer, "0.20mm Standard @InfiMech EX", "0.20mm Standard @InfiMech EX - 3D Print Log.json"],
        [InstallerFixture.SnapmakerOrca, "0.20 Standard @Snapmaker U1 (0.4 nozzle)", "0.20 Standard @Snapmaker U1 (0.4 nozzle) - 3D Print Log.json"],
        [InstallerFixture.AnycubicSlicerNext, "0.20mm Standard @Anycubic Kobra S1 0.4 nozzle", "0.20mm Standard @AC KS1 - 3D Print Log.json"],
    ];

    [TestMethod]
    [DynamicData(nameof(Slicers))]
    public void FindSelectableSystemProfiles_OnRealTree_MatchesSnapshot(string slicer)
    {
        using var fixture = InstallerFixture.Copy(slicer);
        var installer = new FixtureInstaller(fixture);

        var profiles = installer.FindSelectableSystemProfilesForTesting()
            .Select(p => new { p.Name, p.Version, File = Path.GetRelativePath(fixture.Root, p.FilePath).Replace('\\', '/') })
            .OrderBy(p => p.Name, StringComparer.Ordinal)
            .ToList();

        Assert.IsTrue(profiles.Count > 0, "Expected the real tree to contain selectable process profiles.");
        Snapshot.Match(profiles, $"{slicer}-selectable-profiles");
    }

    [TestMethod]
    [DynamicData(nameof(Slicers))]
    public void Install_OnRealTree_AccountsForEveryProfileInEveryAccount(string slicer)
    {
        using var fixture = InstallerFixture.Copy(slicer);
        var installer = new FixtureInstaller(fixture);
        var systemProfiles = installer.FindSelectableSystemProfilesForTesting();
        var accounts = fixture.AccountDirs().ToList();

        var result = installer.Install(ExePath, Flags, dryRun: false);

        Assert.AreEqual(systemProfiles.Count * accounts.Count, result.Created + result.Updated + result.Skipped + result.CoveredByHandMade,
            "Every (system profile × user account) pair must be accounted for exactly once.");
        Assert.AreEqual(0, result.Unreadable);
        Assert.AreEqual(InstallerFixture.WithHandMadeProfiles.Contains(slicer), result.CoveredByHandMade > 0);

        foreach (string account in accounts)
        {
            var coveredParents = HandMadeUploaderProfiles(Path.Combine(account, "process"))
                .Select(p => p["inherits"]!.GetValue<string>())
                .ToHashSet();

            foreach (var profile in systemProfiles)
            {
                string overridePath = Path.Combine(account, "process", profile.Name + Suffix + ".json");
                if (coveredParents.Contains(profile.Name))
                {
                    Assert.IsFalse(File.Exists(overridePath), $"'{profile.Name}' is covered by a hand-made profile; {overridePath} would be a duplicate.");
                    continue;
                }

                Assert.IsTrue(File.Exists(overridePath), $"Missing override: {overridePath}");
                var node = JsonNode.Parse(File.ReadAllText(overridePath))!.AsObject();
                Assert.AreEqual(profile.Name, node["inherits"]!.GetValue<string>());
                Assert.AreEqual(profile.Name + Suffix, node["name"]!.GetValue<string>());
                Assert.AreEqual(profile.Name + Suffix, node["print_settings_id"]!.GetValue<string>());
                Assert.AreEqual("User", node["from"]!.GetValue<string>());
                CollectionAssert.AreEqual(
                    new[] { ExpectedEntry },
                    node["post_process"]!.AsArray().Select(e => e!.GetValue<string>()).ToArray(),
                    $"post_process of {overridePath}");
            }
        }
    }

    [TestMethod]
    [DynamicData(nameof(SlicersWithHandMadeProfiles))]
    public void Install_OnRealTree_OnlyEditsTheUploaderPathInsidePreExistingFiles(string slicer)
    {
        using var fixture = InstallerFixture.Copy(slicer);
        var before = ReadAllText(fixture);

        var result = new FixtureInstaller(fixture).Install(ExePath, Flags, dryRun: false);

        var after = ReadAllText(fixture);
        int refreshed = 0;
        foreach (var (path, text) in before)
        {
            Assert.IsTrue(after.ContainsKey(path), $"Install deleted {path}");
            if (after[path] == text) continue;

            refreshed++;
            AssertOnlyUploaderPathChanged(path, text, after[path]);
        }
        Assert.AreEqual(result.HandMadeRefreshed, refreshed, "Every changed pre-existing file must be reported as a refreshed hand-made profile.");
        Assert.IsTrue(refreshed > 0, "Every fixture carries hand-made profiles pointing at an old executable.");
    }

    [TestMethod]
    [DynamicData(nameof(Slicers))]
    public void Install_OnRealTree_IsIdempotent(string slicer)
    {
        using var fixture = InstallerFixture.Copy(slicer);
        var installer = new FixtureInstaller(fixture);
        var first = installer.Install(ExePath, Flags, dryRun: false);
        var afterFirst = fixture.Snapshot();

        var second = installer.Install(ExePath, Flags, dryRun: false);

        Assert.AreEqual(0, second.Created);
        Assert.AreEqual(0, second.Updated);
        Assert.AreEqual(0, second.HandMadeRefreshed);
        Assert.AreEqual(first.Created + first.Updated + first.Skipped, second.Skipped);
        Assert.AreEqual(first.CoveredByHandMade, second.CoveredByHandMade);
        CollectionAssert.AreEqual(afterFirst.ToList(), fixture.Snapshot().ToList(), "Second install changed files.");
    }

    [TestMethod]
    [DynamicData(nameof(Slicers))]
    public void Install_OnRealTree_DryRunWritesNothingButReportsTheRealRun(string slicer)
    {
        using var fixture = InstallerFixture.Copy(slicer);
        var before = fixture.Snapshot();

        var dry = new FixtureInstaller(fixture).Install(ExePath, Flags, dryRun: true);
        CollectionAssert.AreEqual(before.ToList(), fixture.Snapshot().ToList(), "Dry run wrote files.");

        var wet = new FixtureInstaller(fixture).Install(ExePath, Flags, dryRun: false);
        Assert.AreEqual(wet, dry, "Dry run must report exactly what the real run then does.");
    }

    [TestMethod]
    [DynamicData(nameof(Slicers))]
    public void GetInstallStatus_OnRealTree_AfterInstallReportsOwnedOverridesAndFlags(string slicer)
    {
        using var fixture = InstallerFixture.Copy(slicer);
        var installer = new FixtureInstaller(fixture);
        Assert.IsFalse(installer.GetInstallStatus(ExePath).IsInstalled, "Fixture must start without installer-created overrides for current system profiles.");

        var result = installer.Install(ExePath, Flags, dryRun: false);
        var status = installer.GetInstallStatus(ExePath);

        Assert.IsTrue(status.IsInstalled);
        Assert.AreEqual(Flags, status.InstalledFlags);
        Assert.AreEqual(result.Created + result.Updated + result.Skipped, status.InstalledCount);
    }

    [TestMethod]
    [DynamicData(nameof(Slicers))]
    public void Uninstall_OnRealTree_AfterInstallRemovesOnlyInstallerOwnedFiles(string slicer)
    {
        using var fixture = InstallerFixture.Copy(slicer);
        var installer = new FixtureInstaller(fixture);
        var installed = installer.Install(ExePath, Flags, dryRun: false);
        var afterInstall = fixture.Snapshot();
        int ownedBefore = afterInstall.Keys.Count(IsOwnedOverride);

        var removed = installer.Uninstall(ExePath, dryRun: false);

        var afterUninstall = fixture.Snapshot();
        Assert.AreEqual(ownedBefore, removed.RemovedFiles + removed.ModifiedFiles, "Every owned override must be handled.");
        Assert.IsTrue(removed.RemovedFiles >= installed.Created, "Everything the installer created must be deleted again.");
        CollectionAssert.AreEqual(
            afterInstall.Where(kv => !IsOwnedOverride(kv.Key)).ToList(),
            afterUninstall.Where(kv => !IsOwnedOverride(kv.Key)).ToList(),
            "Everything the installer does not own must be byte-identical after uninstall.");

        // An owned override survives only when the user added their own settings to it (e.g. the fixture's
        // "0.20mm HIgh Speed @SK1 - 3DPrintLog" with tuned speeds); it must then no longer call the uploader.
        var survivors = afterUninstall.Keys.Where(IsOwnedOverride).ToList();
        Assert.AreEqual(removed.ModifiedFiles, survivors.Count);
        foreach (string survivor in survivors)
        {
            var node = JsonNode.Parse(File.ReadAllText(Path.Combine(fixture.Root, survivor)))!.AsObject();
            Assert.IsFalse(node["post_process"]?.AsArray().Any(e => e!.GetValue<string>().Contains("Slic3rPostProcessingUploader")) == true, $"{survivor} still calls the uploader");
            Assert.IsTrue(node.Any(kv => kv.Key is not ("from" or "inherits" or "name" or "print_settings_id" or "version" or "is_custom_defined")), $"{survivor} has no user settings, so it should have been deleted");
        }
    }

    [TestMethod]
    [DynamicData(nameof(SlicersWithHandMadeProfiles))]
    public void Uninstall_OnRealTree_WithoutPriorInstallLeavesHandMadeProfilesAndReportsThem(string slicer)
    {
        using var fixture = InstallerFixture.Copy(slicer);
        var before = fixture.Snapshot();
        int handMade = fixture.AccountDirs().Sum(a => HandMadeUploaderProfiles(Path.Combine(a, "process")).Count);

        var result = new FixtureInstaller(fixture).Uninstall(ExePath, dryRun: false);

        Assert.AreEqual(handMade, result.HandMadeLeft);
        Assert.IsTrue(handMade > 0, "Every fixture carries hand-made uploader profiles.");
        // The only files that may disappear are pre-existing " - 3DPrintLog" overrides (e.g. ones whose
        // vendor has since been removed); by name those are the installer's to clean up.
        CollectionAssert.AreEqual(
            before.Where(kv => !IsOwnedOverride(kv.Key)).ToList(),
            fixture.Snapshot().Where(kv => !IsOwnedOverride(kv.Key)).ToList());
    }

    [TestMethod]
    [DynamicData(nameof(HandMadeProfiles))]
    public void Install_OnRealTree_DoesNotDuplicateHandMadeProfileThatAlreadyRunsUploader(string slicer, string systemProfile, string handMadeFile)
    {
        using var fixture = InstallerFixture.Copy(slicer);
        string handMadePath = Path.Combine(fixture.UserProcessDir(), handMadeFile);
        string handMadeBefore = File.ReadAllText(handMadePath);
        var handMade = JsonNode.Parse(handMadeBefore)!.AsObject();
        Assert.AreEqual(systemProfile, handMade["inherits"]!.GetValue<string>(), "fixture sanity");
        StringAssert.Contains(handMade["post_process"]![0]!.GetValue<string>(), "Slic3rPostProcessingUploader", "fixture sanity");

        new FixtureInstaller(fixture).Install(ExePath, Flags, dryRun: false);

        AssertOnlyUploaderPathChanged(handMadePath, handMadeBefore, File.ReadAllText(handMadePath));
        Assert.IsFalse(File.Exists(Path.Combine(fixture.UserProcessDir(), systemProfile + Suffix + ".json")),
            $"'{systemProfile}' is already covered by the hand-made '{handMadeFile}'; a second '{Suffix}' override would show up as a duplicate in the slicer.");
    }

    // ---- helpers ----

    private static bool IsOwnedOverride(string relativePath) =>
        relativePath.StartsWith("user/") && relativePath.EndsWith(Suffix + ".json", StringComparison.Ordinal);

    private static IReadOnlyDictionary<string, string> ReadAllText(InstallerFixture fixture) =>
        Directory.EnumerateFiles(fixture.Root, "*", SearchOption.AllDirectories)
            .ToDictionary(f => Path.GetRelativePath(fixture.Root, f).Replace('\\', '/'), File.ReadAllText);

    private static List<JsonObject> HandMadeUploaderProfiles(string processDir) =>
        Directory.EnumerateFiles(processDir, "*.json")
            .Where(f => !IsOwnedOverride("user/" + Path.GetFileName(f)))
            .Select(f => JsonNode.Parse(File.ReadAllText(f))!.AsObject())
            .Where(n => n["post_process"]?.AsArray().Any(e => e!.GetValue<string>().Contains("Slic3rPostProcessingUploader")) == true)
            .ToList();

    /// <summary>
    /// The only permitted edit to a hand-made profile: the uploader entry now points at <see cref="ExePath"/>,
    /// with the user's own flags intact, and every other field is exactly as it was.
    /// </summary>
    private static void AssertOnlyUploaderPathChanged(string path, string beforeText, string afterText)
    {
        Assert.IsFalse(IsOwnedOverride("user/" + Path.GetFileName(path)), $"{path} is installer-owned, not hand-made.");

        var before = JsonNode.Parse(beforeText)!.AsObject();
        var after = JsonNode.Parse(afterText)!.AsObject();
        CollectionAssert.AreEqual(before.Select(kv => kv.Key).ToList(), after.Select(kv => kv.Key).ToList(), $"{path}: keys changed");

        foreach (var (key, value) in before)
        {
            if (key == "post_process") continue;
            Assert.AreEqual(value!.ToJsonString(), after[key]!.ToJsonString(), $"{path}: '{key}' changed");
        }

        var entriesBefore = before["post_process"]!.AsArray().Select(e => e!.GetValue<string>()).ToList();
        var entriesAfter = after["post_process"]!.AsArray().Select(e => e!.GetValue<string>()).ToList();
        Assert.AreEqual(entriesBefore.Count, entriesAfter.Count, $"{path}: post_process length changed");
        for (int i = 0; i < entriesBefore.Count; i++)
        {
            if (!entriesBefore[i].Contains("Slic3rPostProcessingUploader"))
            {
                Assert.AreEqual(entriesBefore[i], entriesAfter[i], $"{path}: foreign post_process entry changed");
                continue;
            }

            string oldFlags = entriesBefore[i].Contains("\" ") ? entriesBefore[i][(entriesBefore[i].IndexOf("\" ") + 2)..] : entriesBefore[i].Contains(' ') ? entriesBefore[i][(entriesBefore[i].IndexOf(' ') + 1)..] : "";
            string expected = string.IsNullOrWhiteSpace(oldFlags) ? $"\"{ExePath}\"" : $"\"{ExePath}\" {oldFlags.Trim()}";
            Assert.AreEqual(expected, entriesAfter[i], $"{path}: uploader entry not refreshed as expected");
        }
    }
}
