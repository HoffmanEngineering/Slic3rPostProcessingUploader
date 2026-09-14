# Slicer Profile Installer Implementation Plan

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** Add an interactive install/uninstall wizard that injects the uploader executable path into all OrcaSlicer-family process profiles, so users never have to manually configure post-process scripts.

**Architecture:** A new `ISlicerProfileInstaller` interface abstracts each slicer family. An `OrcaFamilyProfileInstaller` base class handles the shared JSON profile logic for OrcaSlicer, Snapmaker_Orca, and AnycubicSlicerNext. A `WizardService` drives the interactive terminal flow. When the binary is launched with no arguments (double-click), it enters wizard mode instead of the normal post-process flow.

**Tech Stack:** .NET 10, MSTest, `System.Text.Json` with `JsonNode` (NativeAOT-safe, no reflection), `RuntimeInformation` for OS detection. No new NuGet packages required.

**NativeAOT constraint:** The project publishes with `PublishAot>true`. Never use `JsonSerializer.Deserialize<T>()` with a plain generic type — use `JsonNode` for all profile JSON reading/writing, as it is reflection-free and NativeAOT-compatible.

---

### Task 1: AppMode enum + extend ArgumentParser

Add install/uninstall mode detection to `ArgumentParser` without breaking existing behavior.

**Files:**
- Create: `Slic3rPostProcessingUploader/Services/AppMode.cs`
- Modify: `Slic3rPostProcessingUploader/Services/ArgumentParser.cs`
- Modify: `Slic3rPostProcessingUploaderUnitTests/Services/ArgumentParserTests.cs`

**Step 1: Write the failing tests**

Add to `ArgumentParserTests.cs`:

```csharp
#region AppMode Tests

[TestMethod]
public void Constructor_WithNoArguments_SetsWizardMode()
{
    var parser = new ArgumentParser([]);
    Assert.AreEqual(AppMode.Wizard, parser.Mode);
}

[TestMethod]
public void Constructor_WithInstallSubcommand_SetsInstallMode()
{
    var parser = new ArgumentParser(["install"]);
    Assert.AreEqual(AppMode.Install, parser.Mode);
}

[TestMethod]
public void Constructor_WithUninstallSubcommand_SetsUninstallMode()
{
    var parser = new ArgumentParser(["uninstall"]);
    Assert.AreEqual(AppMode.Uninstall, parser.Mode);
}

[TestMethod]
public void Constructor_WithInstallAndDryRun_SetsDryRun()
{
    var parser = new ArgumentParser(["install", "--dry-run"]);
    Assert.AreEqual(AppMode.Install, parser.Mode);
    Assert.IsTrue(parser.IsDryRun);
}

[TestMethod]
public void Constructor_WithUninstallAndDryRun_SetsDryRun()
{
    var parser = new ArgumentParser(["uninstall", "--dry-run"]);
    Assert.AreEqual(AppMode.Uninstall, parser.Mode);
    Assert.IsTrue(parser.IsDryRun);
}

[TestMethod]
public void Constructor_WithGcodeFile_SetsPostProcessMode()
{
    var parser = new ArgumentParser(["--full", "myfile.gcode"]);
    Assert.AreEqual(AppMode.PostProcess, parser.Mode);
}

[TestMethod]
public void Constructor_WithInstallMode_IsDryRunDefaultsFalse()
{
    var parser = new ArgumentParser(["install"]);
    Assert.IsFalse(parser.IsDryRun);
}

#endregion
```

**Step 2: Run tests to verify they fail**

```bash
cd Slic3rPostProcessingUploaderUnitTests
dotnet test --filter "FullyQualifiedName~ArgumentParserTests" -v
```

Expected: FAIL — `AppMode` and `Mode`/`IsDryRun` don't exist yet.

**Step 3: Create AppMode enum**

Create `Slic3rPostProcessingUploader/Services/AppMode.cs`:

```csharp
namespace Slic3rPostProcessingUploader.Services
{
    internal enum AppMode
    {
        PostProcess,
        Wizard,
        Install,
        Uninstall
    }
}
```

**Step 4: Extend ArgumentParser**

Add to `ArgumentParser` class (before the constructor):

```csharp
public AppMode Mode { get; private set; }
public bool IsDryRun { get; private set; }
```

Replace the constructor body with this logic (the existing flag parsing stays intact — just add mode detection at the top):

```csharp
public ArgumentParser(string[] args) {
    this.UseDefaultNoteTemplate = true;
    this.UseFullNoteTemplate = false;
    this.DisableTelemetry = false;
    this.DisplayHelp = false;

    // Detect install/uninstall sub-commands first
    if (args.Length == 0)
    {
        this.Mode = AppMode.Wizard;
        return;
    }

    if (args[0] == "install")
    {
        this.Mode = AppMode.Install;
        this.IsDryRun = args.Contains("--dry-run");
        return;
    }

    if (args[0] == "uninstall")
    {
        this.Mode = AppMode.Uninstall;
        this.IsDryRun = args.Contains("--dry-run");
        return;
    }

    this.Mode = AppMode.PostProcess;

    // ... rest of existing constructor body unchanged ...
```

**Step 5: Run tests to verify they pass**

```bash
dotnet test --filter "FullyQualifiedName~ArgumentParserTests" -v
```

Expected: All ArgumentParser tests PASS.

**Step 6: Commit**

```bash
git add Slic3rPostProcessingUploader/Services/AppMode.cs Slic3rPostProcessingUploader/Services/ArgumentParser.cs Slic3rPostProcessingUploaderUnitTests/Services/ArgumentParserTests.cs
git commit -m "feat: add AppMode enum and install/uninstall mode detection to ArgumentParser"
```

---

### Task 2: Result types + ISlicerProfileInstaller interface

Define the data contracts used by all slicer installer implementations.

**Files:**
- Create: `Slic3rPostProcessingUploader/Services/Installer/SlicerInstallStatus.cs`
- Create: `Slic3rPostProcessingUploader/Services/Installer/InstallResult.cs`
- Create: `Slic3rPostProcessingUploader/Services/Installer/ISlicerProfileInstaller.cs`

No tests needed for these — they are pure data contracts with no logic. They will be tested through the implementations in later tasks.

**Step 1: Create SlicerInstallStatus**

Create `Slic3rPostProcessingUploader/Services/Installer/SlicerInstallStatus.cs`:

```csharp
namespace Slic3rPostProcessingUploader.Services.Installer
{
    internal record SlicerInstallStatus(
        bool IsInstalled,
        int ProfileCount,       // total selectable process profiles found
        int InstalledCount,     // profiles already containing our script
        string? InstalledFlags  // flags detected in existing install, e.g. "--full"
    );
}
```

**Step 2: Create InstallResult**

Create `Slic3rPostProcessingUploader/Services/Installer/InstallResult.cs`:

```csharp
namespace Slic3rPostProcessingUploader.Services.Installer
{
    internal record InstallResult(
        int Created,            // new user override files created
        int Updated,            // existing user overrides updated
        int Skipped,            // profiles already up to date
        int WithOtherScripts,   // profiles that had other post-process scripts (appended alongside)
        int RemovedFiles,       // (uninstall only) user override files deleted
        int ModifiedFiles       // (uninstall only) files kept but our entry removed
    );
}
```

**Step 3: Create ISlicerProfileInstaller**

Create `Slic3rPostProcessingUploader/Services/Installer/ISlicerProfileInstaller.cs`:

```csharp
namespace Slic3rPostProcessingUploader.Services.Installer
{
    internal interface ISlicerProfileInstaller
    {
        /// <summary>Display name shown to the user, e.g. "OrcaSlicer"</summary>
        string SlicerName { get; }

        /// <summary>Returns true if the slicer's config directory exists on this machine.</summary>
        bool IsDetected();

        /// <summary>Scans installed profiles to determine current installation state.</summary>
        SlicerInstallStatus GetInstallStatus(string executablePath);

        /// <summary>Injects the executable path into all selectable process profiles.</summary>
        InstallResult Install(string executablePath, string flags, bool dryRun);

        /// <summary>Removes the executable path from all process profiles.</summary>
        InstallResult Uninstall(string executablePath, bool dryRun);
    }
}
```

**Step 4: Commit**

```bash
git add Slic3rPostProcessingUploader/Services/Installer/
git commit -m "feat: add ISlicerProfileInstaller interface and result types"
```

---

### Task 3: OrcaFamilyProfileInstaller — config root resolution + IsDetected

The OS-aware path resolver is the foundation for all OrcaFamily operations.

**Files:**
- Create: `Slic3rPostProcessingUploader/Services/Installer/OrcaFamily/OrcaFamilyProfileInstaller.cs`
- Create: `Slic3rPostProcessingUploaderUnitTests/Services/Installer/OrcaFamily/OrcaFamilyProfileInstallerTests.cs`

**Step 1: Write the failing tests**

Create `Slic3rPostProcessingUploaderUnitTests/Services/Installer/OrcaFamily/OrcaFamilyProfileInstallerTests.cs`:

```csharp
using Slic3rPostProcessingUploader.Services.Installer.OrcaFamily;
using System.Runtime.InteropServices;

namespace Slic3rPostProcessingUploaderUnitTests.Services.Installer.OrcaFamily
{
    [TestClass]
    public class OrcaFamilyProfileInstallerTests
    {
        // Concrete subclass for testing the abstract base
        private class TestOrcaInstaller : OrcaFamilyProfileInstaller
        {
            public override string SlicerName => "TestSlicer";
            protected override string SlicerDirectoryName => "TestSlicer";
        }

        [TestMethod]
        public void IsDetected_WhenConfigDirDoesNotExist_ReturnsFalse()
        {
            var installer = new TestOrcaInstaller();
            // Config root won't exist for "TestSlicer" on any real machine
            Assert.IsFalse(installer.IsDetected());
        }

        [TestMethod]
        public void IsDetected_WhenConfigDirExists_ReturnsTrue()
        {
            var tempDir = Path.Combine(Path.GetTempPath(), "TestSlicer_" + Guid.NewGuid());
            Directory.CreateDirectory(tempDir);

            try
            {
                var installer = new TestOrcaInstaller(configRootOverride: tempDir);
                Assert.IsTrue(installer.IsDetected());
            }
            finally
            {
                Directory.Delete(tempDir, recursive: true);
            }
        }

        [TestMethod]
        public void GetConfigRoot_ReturnsPathContainingSlicerName()
        {
            var installer = new TestOrcaInstaller();
            var root = installer.GetConfigRootForTesting();
            Assert.IsTrue(root.Contains("TestSlicer"));
        }
    }
}
```

**Step 2: Run tests to verify they fail**

```bash
dotnet test --filter "FullyQualifiedName~OrcaFamilyProfileInstallerTests" -v
```

Expected: FAIL — `OrcaFamilyProfileInstaller` doesn't exist.

**Step 3: Create OrcaFamilyProfileInstaller with config root logic**

Create `Slic3rPostProcessingUploader/Services/Installer/OrcaFamily/OrcaFamilyProfileInstaller.cs`:

```csharp
using System.Runtime.InteropServices;

namespace Slic3rPostProcessingUploader.Services.Installer.OrcaFamily
{
    internal abstract class OrcaFamilyProfileInstaller : ISlicerProfileInstaller
    {
        private readonly string? _configRootOverride;

        public abstract string SlicerName { get; }
        protected abstract string SlicerDirectoryName { get; }

        protected OrcaFamilyProfileInstaller(string? configRootOverride = null)
        {
            _configRootOverride = configRootOverride;
        }

        protected string GetConfigRoot()
        {
            if (_configRootOverride != null)
                return _configRootOverride;

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                return Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                    SlicerDirectoryName);

            if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                return Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.Personal),
                    "Library", "Application Support", SlicerDirectoryName);

            // Linux: respect XDG_CONFIG_HOME
            string? xdg = Environment.GetEnvironmentVariable("XDG_CONFIG_HOME");
            string baseDir = !string.IsNullOrEmpty(xdg)
                ? xdg
                : Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Personal), ".config");
            return Path.Combine(baseDir, SlicerDirectoryName);
        }

        // Exposed for testing only
        internal string GetConfigRootForTesting() => GetConfigRoot();

        public bool IsDetected() => Directory.Exists(GetConfigRoot());

        public SlicerInstallStatus GetInstallStatus(string executablePath) => throw new NotImplementedException();
        public InstallResult Install(string executablePath, string flags, bool dryRun) => throw new NotImplementedException();
        public InstallResult Uninstall(string executablePath, bool dryRun) => throw new NotImplementedException();
    }
}
```

**Step 4: Run tests to verify they pass**

```bash
dotnet test --filter "FullyQualifiedName~OrcaFamilyProfileInstallerTests" -v
```

Expected: All 3 tests PASS.

**Step 5: Commit**

```bash
git add Slic3rPostProcessingUploader/Services/Installer/OrcaFamily/OrcaFamilyProfileInstaller.cs Slic3rPostProcessingUploaderUnitTests/Services/Installer/OrcaFamily/OrcaFamilyProfileInstallerTests.cs
git commit -m "feat: add OrcaFamilyProfileInstaller with OS-aware config root resolution"
```

---

### Task 4: OrcaFamilyProfileInstaller — system profile discovery

Scan the `system/` directory tree to find all selectable (non-abstract) process profiles, and enumerate all user account subdirectories.

**Files:**
- Modify: `Slic3rPostProcessingUploader/Services/Installer/OrcaFamily/OrcaFamilyProfileInstaller.cs`
- Modify: `Slic3rPostProcessingUploaderUnitTests/Services/Installer/OrcaFamily/OrcaFamilyProfileInstallerTests.cs`

**Step 1: Write the failing tests**

Add to `OrcaFamilyProfileInstallerTests.cs`:

```csharp
// Helper to build a minimal temp config dir with system profiles
private static string BuildTempConfigDir(params (string name, bool instantiation)[] profiles)
{
    var root = Path.Combine(Path.GetTempPath(), "TestSlicer_" + Guid.NewGuid());
    var processDir = Path.Combine(root, "system", "Vendor", "process");
    Directory.CreateDirectory(processDir);
    Directory.CreateDirectory(Path.Combine(root, "user", "default", "process"));

    foreach (var (name, instantiation) in profiles)
    {
        File.WriteAllText(
            Path.Combine(processDir, $"{name}.json"),
            $$"""{"name": "{{name}}", "instantiation": "{{(instantiation ? "true" : "false")}}", "from": "system", "version": "1.0.0"}""");
    }

    return root;
}

[TestMethod]
public void FindSelectableSystemProfiles_ReturnsOnlyInstantiationTrue()
{
    var root = BuildTempConfigDir(
        ("0.20mm Standard @Printer", true),
        ("fdm_process_common", false),
        ("0.10mm Fine @Printer", true)
    );

    try
    {
        var installer = new TestOrcaInstaller(configRootOverride: root);
        var profiles = installer.FindSelectableSystemProfilesForTesting();
        Assert.AreEqual(2, profiles.Count);
        Assert.IsTrue(profiles.Any(p => p.Name == "0.20mm Standard @Printer"));
        Assert.IsTrue(profiles.Any(p => p.Name == "0.10mm Fine @Printer"));
    }
    finally
    {
        Directory.Delete(root, recursive: true);
    }
}

[TestMethod]
public void FindUserAccountDirs_ReturnsAllSubdirectories()
{
    var root = BuildTempConfigDir();
    Directory.CreateDirectory(Path.Combine(root, "user", "56914", "process"));
    Directory.CreateDirectory(Path.Combine(root, "user", "99999", "process"));

    try
    {
        var installer = new TestOrcaInstaller(configRootOverride: root);
        var dirs = installer.FindUserAccountDirsForTesting();
        Assert.AreEqual(3, dirs.Count); // default + 56914 + 99999
    }
    finally
    {
        Directory.Delete(root, recursive: true);
    }
}
```

**Step 2: Run tests to verify they fail**

```bash
dotnet test --filter "FullyQualifiedName~OrcaFamilyProfileInstallerTests" -v
```

Expected: FAIL — `FindSelectableSystemProfilesForTesting` and `FindUserAccountDirsForTesting` don't exist.

**Step 3: Add SystemProfile record and discovery methods**

Add inside `OrcaFamilyProfileInstaller.cs` (before the closing brace of the class):

```csharp
internal record SystemProfile(string Name, string FilePath, string Version);

protected List<SystemProfile> FindSelectableSystemProfiles()
{
    var result = new List<SystemProfile>();
    var systemDir = Path.Combine(GetConfigRoot(), "system");
    if (!Directory.Exists(systemDir)) return result;

    foreach (var file in Directory.EnumerateFiles(systemDir, "*.json", SearchOption.AllDirectories))
    {
        // Only files in a "process" subdirectory
        if (!file.Contains(Path.DirectorySeparatorChar + "process" + Path.DirectorySeparatorChar))
            continue;

        try
        {
            var text = File.ReadAllText(file);
            var node = System.Text.Json.Nodes.JsonNode.Parse(text);
            if (node == null) continue;

            var instantiation = node["instantiation"]?.GetValue<string>();
            if (instantiation != "true") continue;

            var name = node["name"]?.GetValue<string>();
            var version = node["version"]?.GetValue<string>() ?? "";
            if (string.IsNullOrEmpty(name)) continue;

            result.Add(new SystemProfile(name, file, version));
        }
        catch { /* skip malformed JSON */ }
    }

    return result;
}

protected List<string> FindUserAccountDirs()
{
    var userDir = Path.Combine(GetConfigRoot(), "user");
    if (!Directory.Exists(userDir)) return new List<string>();
    return Directory.GetDirectories(userDir).ToList();
}

// Exposed for testing only
internal List<SystemProfile> FindSelectableSystemProfilesForTesting() => FindSelectableSystemProfiles();
internal List<string> FindUserAccountDirsForTesting() => FindUserAccountDirs();
```

**Step 4: Run tests to verify they pass**

```bash
dotnet test --filter "FullyQualifiedName~OrcaFamilyProfileInstallerTests" -v
```

Expected: All tests PASS.

**Step 5: Commit**

```bash
git add Slic3rPostProcessingUploader/Services/Installer/OrcaFamily/OrcaFamilyProfileInstaller.cs Slic3rPostProcessingUploaderUnitTests/Services/Installer/OrcaFamily/OrcaFamilyProfileInstallerTests.cs
git commit -m "feat: add system profile discovery and user account dir enumeration"
```

---

### Task 5: OrcaFamilyProfileInstaller — Install (injection) logic

The core feature: create or update user override JSON files with the `post_process` entry.

**Files:**
- Modify: `Slic3rPostProcessingUploader/Services/Installer/OrcaFamily/OrcaFamilyProfileInstaller.cs`
- Modify: `Slic3rPostProcessingUploaderUnitTests/Services/Installer/OrcaFamily/OrcaFamilyProfileInstallerTests.cs`

**Step 1: Write the failing tests**

Add to `OrcaFamilyProfileInstallerTests.cs`:

```csharp
private static string BuildTempConfigDirWithUserOverride(
    string profileName,
    string? existingPostProcess = null,
    Dictionary<string, string>? extraFields = null)
{
    var root = Path.Combine(Path.GetTempPath(), "TestSlicer_" + Guid.NewGuid());
    var systemProcessDir = Path.Combine(root, "system", "Vendor", "process");
    var userProcessDir = Path.Combine(root, "user", "default", "process");
    Directory.CreateDirectory(systemProcessDir);
    Directory.CreateDirectory(userProcessDir);

    File.WriteAllText(
        Path.Combine(systemProcessDir, $"{profileName}.json"),
        $$"""{"name": "{{profileName}}", "instantiation": "true", "from": "system", "version": "2.0.0"}""");

    if (existingPostProcess != null || extraFields != null)
    {
        var node = new System.Text.Json.Nodes.JsonObject
        {
            ["from"] = "User",
            ["inherits"] = profileName,
            ["name"] = profileName,
            ["print_settings_id"] = profileName,
            ["version"] = "2.0.0"
        };
        if (existingPostProcess != null)
        {
            node["post_process"] = new System.Text.Json.Nodes.JsonArray(
                System.Text.Json.Nodes.JsonValue.Create(existingPostProcess));
        }
        if (extraFields != null)
        {
            foreach (var kv in extraFields)
                node[kv.Key] = kv.Value;
        }
        File.WriteAllText(
            Path.Combine(userProcessDir, $"{profileName}.json"),
            node.ToJsonString());
    }

    return root;
}

[TestMethod]
public void Install_CreatesUserOverride_WhenNoneExists()
{
    var root = BuildTempConfigDirWithUserOverride("0.20mm Standard @Printer");
    try
    {
        var installer = new TestOrcaInstaller(configRootOverride: root);
        var result = installer.Install("C:\\uploader.exe", "--full", dryRun: false);

        Assert.AreEqual(1, result.Created);
        Assert.AreEqual(0, result.Updated);

        var overridePath = Path.Combine(root, "user", "default", "process", "0.20mm Standard @Printer.json");
        Assert.IsTrue(File.Exists(overridePath));

        var node = System.Text.Json.Nodes.JsonNode.Parse(File.ReadAllText(overridePath));
        var postProcess = node!["post_process"]!.AsArray();
        Assert.AreEqual(1, postProcess.Count);
        Assert.IsTrue(postProcess[0]!.GetValue<string>().Contains("uploader.exe"));
    }
    finally { Directory.Delete(root, recursive: true); }
}

[TestMethod]
public void Install_UpdatesUserOverride_WhenOverrideExistsWithoutOurScript()
{
    var root = BuildTempConfigDirWithUserOverride("0.20mm Standard @Printer",
        existingPostProcess: "C:\\other-script.exe");
    try
    {
        var installer = new TestOrcaInstaller(configRootOverride: root);
        var result = installer.Install("C:\\uploader.exe", "--full", dryRun: false);

        Assert.AreEqual(0, result.Created);
        Assert.AreEqual(1, result.Updated);
        Assert.AreEqual(1, result.WithOtherScripts);

        var overridePath = Path.Combine(root, "user", "default", "process", "0.20mm Standard @Printer.json");
        var node = System.Text.Json.Nodes.JsonNode.Parse(File.ReadAllText(overridePath));
        var postProcess = node!["post_process"]!.AsArray();
        Assert.AreEqual(2, postProcess.Count); // other script + ours
    }
    finally { Directory.Delete(root, recursive: true); }
}

[TestMethod]
public void Install_SkipsProfile_WhenOurScriptAlreadyPresent()
{
    var root = BuildTempConfigDirWithUserOverride("0.20mm Standard @Printer",
        existingPostProcess: "C:\\Slic3rPostProcessingUploader.exe --full");
    try
    {
        var installer = new TestOrcaInstaller(configRootOverride: root);
        var result = installer.Install("C:\\Slic3rPostProcessingUploader.exe", "--full", dryRun: false);

        Assert.AreEqual(0, result.Created);
        Assert.AreEqual(0, result.Updated);
        Assert.AreEqual(1, result.Skipped);
    }
    finally { Directory.Delete(root, recursive: true); }
}

[TestMethod]
public void Install_DryRun_WritesNoFiles()
{
    var root = BuildTempConfigDirWithUserOverride("0.20mm Standard @Printer");
    try
    {
        var installer = new TestOrcaInstaller(configRootOverride: root);
        installer.Install("C:\\uploader.exe", "--full", dryRun: true);

        var overridePath = Path.Combine(root, "user", "default", "process", "0.20mm Standard @Printer.json");
        Assert.IsFalse(File.Exists(overridePath));
    }
    finally { Directory.Delete(root, recursive: true); }
}

[TestMethod]
public void Install_QuotesExePath_WhenPathContainsSpaces()
{
    var root = BuildTempConfigDirWithUserOverride("0.20mm Standard @Printer");
    try
    {
        var installer = new TestOrcaInstaller(configRootOverride: root);
        installer.Install("C:\\My Tools\\uploader.exe", "--full", dryRun: false);

        var overridePath = Path.Combine(root, "user", "default", "process", "0.20mm Standard @Printer.json");
        var node = System.Text.Json.Nodes.JsonNode.Parse(File.ReadAllText(overridePath));
        var entry = node!["post_process"]!.AsArray()[0]!.GetValue<string>();
        Assert.IsTrue(entry.StartsWith("\"C:\\My Tools\\uploader.exe\""));
    }
    finally { Directory.Delete(root, recursive: true); }
}
```

**Step 2: Run tests to verify they fail**

```bash
dotnet test --filter "FullyQualifiedName~OrcaFamilyProfileInstallerTests" -v
```

Expected: FAIL — `Install` throws `NotImplementedException`.

**Step 3: Implement Install**

Replace `throw new NotImplementedException()` in `Install` with:

```csharp
public InstallResult Install(string executablePath, string flags, bool dryRun)
{
    string scriptEntry = BuildScriptEntry(executablePath, flags);
    var systemProfiles = FindSelectableSystemProfiles();
    var userAccountDirs = FindUserAccountDirs();

    int created = 0, updated = 0, skipped = 0, withOtherScripts = 0;

    foreach (var systemProfile in systemProfiles)
    {
        foreach (var accountDir in userAccountDirs)
        {
            var processDir = Path.Combine(accountDir, "process");
            var overridePath = Path.Combine(processDir, systemProfile.Name + ".json");

            if (File.Exists(overridePath))
            {
                var node = JsonNode.Parse(File.ReadAllText(overridePath));
                if (node == null) continue;

                var postProcess = node["post_process"]?.AsArray();

                // Check if our script is already present (partial match)
                if (postProcess != null && postProcess.Any(e =>
                    e?.GetValue<string>().Contains("Slic3rPostProcessingUploader") == true))
                {
                    skipped++;
                    continue;
                }

                // Count other scripts
                if (postProcess != null && postProcess.Count > 0)
                    withOtherScripts++;

                if (!dryRun)
                {
                    if (postProcess == null)
                    {
                        node["post_process"] = new JsonArray(JsonValue.Create(scriptEntry));
                    }
                    else
                    {
                        postProcess.Add(JsonValue.Create(scriptEntry));
                    }
                    Directory.CreateDirectory(processDir);
                    File.WriteAllText(overridePath, node.ToJsonString(new JsonSerializerOptions { WriteIndented = true }));
                }
                updated++;
            }
            else
            {
                if (!dryRun)
                {
                    Directory.CreateDirectory(processDir);
                    var newOverride = new JsonObject
                    {
                        ["from"] = "User",
                        ["inherits"] = systemProfile.Name,
                        ["name"] = systemProfile.Name,
                        ["post_process"] = new JsonArray(JsonValue.Create(scriptEntry)),
                        ["print_settings_id"] = systemProfile.Name,
                        ["version"] = systemProfile.Version
                    };
                    File.WriteAllText(overridePath, newOverride.ToJsonString(new JsonSerializerOptions { WriteIndented = true }));
                }
                created++;
            }
        }
    }

    return new InstallResult(created, updated, skipped, withOtherScripts, 0, 0);
}

private static string BuildScriptEntry(string executablePath, string flags)
{
    bool needsQuotes = executablePath.Contains(' ');
    string quotedPath = needsQuotes ? $"\"{executablePath}\"" : executablePath;
    return string.IsNullOrWhiteSpace(flags) ? quotedPath : $"{quotedPath} {flags.Trim()}";
}
```

Add the required using at the top of the file:
```csharp
using System.Text.Json;
using System.Text.Json.Nodes;
```

**Step 4: Run tests to verify they pass**

```bash
dotnet test --filter "FullyQualifiedName~OrcaFamilyProfileInstallerTests" -v
```

Expected: All tests PASS.

**Step 5: Commit**

```bash
git add Slic3rPostProcessingUploader/Services/Installer/OrcaFamily/OrcaFamilyProfileInstaller.cs Slic3rPostProcessingUploaderUnitTests/Services/Installer/OrcaFamily/OrcaFamilyProfileInstallerTests.cs
git commit -m "feat: implement Install method with JSON injection for OrcaFamily profiles"
```

---

### Task 6: OrcaFamilyProfileInstaller — GetInstallStatus

Scan existing user overrides to report current installation state before the wizard prompts.

**Files:**
- Modify: `Slic3rPostProcessingUploader/Services/Installer/OrcaFamily/OrcaFamilyProfileInstaller.cs`
- Modify: `Slic3rPostProcessingUploaderUnitTests/Services/Installer/OrcaFamily/OrcaFamilyProfileInstallerTests.cs`

**Step 1: Write the failing tests**

Add to `OrcaFamilyProfileInstallerTests.cs`:

```csharp
[TestMethod]
public void GetInstallStatus_WhenNotInstalled_ReturnsCorrectStatus()
{
    var root = BuildTempConfigDirWithUserOverride("0.20mm Standard @Printer");
    try
    {
        var installer = new TestOrcaInstaller(configRootOverride: root);
        var status = installer.GetInstallStatus("C:\\uploader.exe");

        Assert.IsFalse(status.IsInstalled);
        Assert.AreEqual(1, status.ProfileCount);
        Assert.AreEqual(0, status.InstalledCount);
        Assert.IsNull(status.InstalledFlags);
    }
    finally { Directory.Delete(root, recursive: true); }
}

[TestMethod]
public void GetInstallStatus_WhenInstalled_ReturnsInstalledCountAndFlags()
{
    var root = BuildTempConfigDirWithUserOverride("0.20mm Standard @Printer",
        existingPostProcess: "\"C:\\uploader.exe\" --full");
    try
    {
        var installer = new TestOrcaInstaller(configRootOverride: root);
        var status = installer.GetInstallStatus("C:\\uploader.exe");

        Assert.IsTrue(status.IsInstalled);
        Assert.AreEqual(1, status.ProfileCount);
        Assert.AreEqual(1, status.InstalledCount);
        Assert.AreEqual("--full", status.InstalledFlags);
    }
    finally { Directory.Delete(root, recursive: true); }
}
```

**Step 2: Run tests to verify they fail**

```bash
dotnet test --filter "FullyQualifiedName~OrcaFamilyProfileInstallerTests" -v
```

Expected: FAIL — `GetInstallStatus` throws `NotImplementedException`.

**Step 3: Implement GetInstallStatus**

Replace the `GetInstallStatus` `throw` with:

```csharp
public SlicerInstallStatus GetInstallStatus(string executablePath)
{
    var systemProfiles = FindSelectableSystemProfiles();
    var userAccountDirs = FindUserAccountDirs();

    int totalProfiles = systemProfiles.Count;
    int installedCount = 0;
    string? detectedFlags = null;

    foreach (var systemProfile in systemProfiles)
    {
        foreach (var accountDir in userAccountDirs)
        {
            var overridePath = Path.Combine(accountDir, "process", systemProfile.Name + ".json");
            if (!File.Exists(overridePath)) continue;

            var node = JsonNode.Parse(File.ReadAllText(overridePath));
            var postProcess = node?["post_process"]?.AsArray();
            if (postProcess == null) continue;

            var ourEntry = postProcess
                .Select(e => e?.GetValue<string>())
                .FirstOrDefault(e => e?.Contains("Slic3rPostProcessingUploader") == true);

            if (ourEntry != null)
            {
                installedCount++;
                if (detectedFlags == null)
                {
                    // Extract flags: everything after the executable path/quoted path
                    var flagsPart = ourEntry.TrimStart('"');
                    var endOfPath = flagsPart.IndexOf('"') >= 0
                        ? flagsPart.IndexOf('"') + 1
                        : (flagsPart.IndexOf(' ') >= 0 ? flagsPart.IndexOf(' ') : flagsPart.Length);
                    var afterPath = ourEntry.Length > endOfPath ? ourEntry[endOfPath..].Trim() : "";
                    detectedFlags = string.IsNullOrWhiteSpace(afterPath) ? null : afterPath;
                }
            }
        }
    }

    return new SlicerInstallStatus(
        IsInstalled: installedCount > 0,
        ProfileCount: totalProfiles,
        InstalledCount: installedCount,
        InstalledFlags: detectedFlags);
}
```

**Step 4: Run tests to verify they pass**

```bash
dotnet test --filter "FullyQualifiedName~OrcaFamilyProfileInstallerTests" -v
```

Expected: All tests PASS.

**Step 5: Commit**

```bash
git add Slic3rPostProcessingUploader/Services/Installer/OrcaFamily/OrcaFamilyProfileInstaller.cs Slic3rPostProcessingUploaderUnitTests/Services/Installer/OrcaFamily/OrcaFamilyProfileInstallerTests.cs
git commit -m "feat: implement GetInstallStatus for OrcaFamily profiles"
```

---

### Task 7: OrcaFamilyProfileInstaller — Uninstall logic

Remove our `post_process` entry from all profiles. Delete the file if it becomes empty; keep it if the user has other customizations.

**Files:**
- Modify: `Slic3rPostProcessingUploader/Services/Installer/OrcaFamily/OrcaFamilyProfileInstaller.cs`
- Modify: `Slic3rPostProcessingUploaderUnitTests/Services/Installer/OrcaFamily/OrcaFamilyProfileInstallerTests.cs`

**Step 1: Write the failing tests**

Add to `OrcaFamilyProfileInstallerTests.cs`:

```csharp
// Fields present in a minimal (empty) override — nothing worth keeping
private static readonly HashSet<string> MinimalOverrideFields = new()
{
    "from", "inherits", "name", "print_settings_id", "version", "is_custom_defined"
};

[TestMethod]
public void Uninstall_DeletesFile_WhenOverrideHasNoOtherCustomizations()
{
    var root = BuildTempConfigDirWithUserOverride("0.20mm Standard @Printer",
        existingPostProcess: "\"C:\\Slic3rPostProcessingUploader.exe\" --full");
    var overridePath = Path.Combine(root, "user", "default", "process", "0.20mm Standard @Printer.json");

    try
    {
        var installer = new TestOrcaInstaller(configRootOverride: root);
        var result = installer.Uninstall("C:\\Slic3rPostProcessingUploader.exe", dryRun: false);

        Assert.IsFalse(File.Exists(overridePath));
        Assert.AreEqual(1, result.RemovedFiles);
        Assert.AreEqual(0, result.ModifiedFiles);
    }
    finally { Directory.Delete(root, recursive: true); }
}

[TestMethod]
public void Uninstall_KeepsFile_WhenOverrideHasOtherCustomizations()
{
    var root = BuildTempConfigDirWithUserOverride("0.20mm Standard @Printer",
        existingPostProcess: "\"C:\\Slic3rPostProcessingUploader.exe\" --full",
        extraFields: new Dictionary<string, string> { ["wall_loops"] = "3" });
    var overridePath = Path.Combine(root, "user", "default", "process", "0.20mm Standard @Printer.json");

    try
    {
        var installer = new TestOrcaInstaller(configRootOverride: root);
        var result = installer.Uninstall("C:\\Slic3rPostProcessingUploader.exe", dryRun: false);

        Assert.IsTrue(File.Exists(overridePath));
        Assert.AreEqual(0, result.RemovedFiles);
        Assert.AreEqual(1, result.ModifiedFiles);

        // Our entry should be gone
        var node = JsonNode.Parse(File.ReadAllText(overridePath));
        var postProcess = node?["post_process"]?.AsArray();
        Assert.IsNull(postProcess); // array removed entirely since it's now empty
    }
    finally { Directory.Delete(root, recursive: true); }
}

[TestMethod]
public void Uninstall_LeavesOtherScripts_WhenProfileHasMultiple()
{
    var root = BuildTempConfigDirWithUserOverride("0.20mm Standard @Printer",
        existingPostProcess: "C:\\other-script.exe");

    // Manually add our script alongside the other
    var overridePath = Path.Combine(root, "user", "default", "process", "0.20mm Standard @Printer.json");
    var node = JsonNode.Parse(File.ReadAllText(overridePath))!;
    node["post_process"]!.AsArray().Add(JsonValue.Create("\"C:\\Slic3rPostProcessingUploader.exe\" --full"));
    File.WriteAllText(overridePath, node.ToJsonString());

    try
    {
        var installer = new TestOrcaInstaller(configRootOverride: root);
        installer.Uninstall("C:\\Slic3rPostProcessingUploader.exe", dryRun: false);

        var result_node = JsonNode.Parse(File.ReadAllText(overridePath));
        var postProcess = result_node!["post_process"]!.AsArray();
        Assert.AreEqual(1, postProcess.Count);
        Assert.IsTrue(postProcess[0]!.GetValue<string>().Contains("other-script.exe"));
    }
    finally { Directory.Delete(root, recursive: true); }
}

[TestMethod]
public void Uninstall_DryRun_WritesNoChanges()
{
    var root = BuildTempConfigDirWithUserOverride("0.20mm Standard @Printer",
        existingPostProcess: "\"C:\\Slic3rPostProcessingUploader.exe\" --full");
    var overridePath = Path.Combine(root, "user", "default", "process", "0.20mm Standard @Printer.json");
    var originalContent = File.ReadAllText(overridePath);

    try
    {
        var installer = new TestOrcaInstaller(configRootOverride: root);
        installer.Uninstall("C:\\Slic3rPostProcessingUploader.exe", dryRun: true);

        Assert.AreEqual(originalContent, File.ReadAllText(overridePath));
    }
    finally { Directory.Delete(root, recursive: true); }
}
```

**Step 2: Run tests to verify they fail**

```bash
dotnet test --filter "FullyQualifiedName~OrcaFamilyProfileInstallerTests" -v
```

Expected: FAIL — `Uninstall` throws `NotImplementedException`.

**Step 3: Implement Uninstall**

Replace the `Uninstall` `throw` with:

```csharp
private static readonly HashSet<string> MinimalOverrideFields = new()
{
    "from", "inherits", "name", "print_settings_id", "version", "is_custom_defined"
};

public InstallResult Uninstall(string executablePath, bool dryRun)
{
    var userAccountDirs = FindUserAccountDirs();
    int removedFiles = 0, modifiedFiles = 0;

    foreach (var accountDir in userAccountDirs)
    {
        var processDir = Path.Combine(accountDir, "process");
        if (!Directory.Exists(processDir)) continue;

        foreach (var file in Directory.EnumerateFiles(processDir, "*.json"))
        {
            var node = JsonNode.Parse(File.ReadAllText(file));
            var postProcess = node?["post_process"]?.AsArray();
            if (postProcess == null) continue;

            // Find our entries
            var ourEntries = postProcess
                .Where(e => e?.GetValue<string>().Contains("Slic3rPostProcessingUploader") == true)
                .ToList();

            if (ourEntries.Count == 0) continue;

            foreach (var entry in ourEntries)
                postProcess.Remove(entry);

            // If post_process is now empty, remove the key entirely
            if (postProcess.Count == 0)
                node!.AsObject().Remove("post_process");

            // Determine if the file has any user customizations worth keeping
            var remainingKeys = node!.AsObject().Select(kv => kv.Key).ToHashSet();
            bool hasCustomizations = remainingKeys.Any(k => !MinimalOverrideFields.Contains(k));

            if (!hasCustomizations)
            {
                if (!dryRun) File.Delete(file);
                removedFiles++;
            }
            else
            {
                if (!dryRun)
                    File.WriteAllText(file, node.ToJsonString(new JsonSerializerOptions { WriteIndented = true }));
                modifiedFiles++;
            }
        }
    }

    return new InstallResult(0, 0, 0, 0, removedFiles, modifiedFiles);
}
```

**Step 4: Run tests to verify they pass**

```bash
dotnet test --filter "FullyQualifiedName~OrcaFamilyProfileInstallerTests" -v
```

Expected: All tests PASS.

**Step 5: Run full test suite**

```bash
dotnet test -v
```

Expected: All tests PASS.

**Step 6: Commit**

```bash
git add Slic3rPostProcessingUploader/Services/Installer/OrcaFamily/OrcaFamilyProfileInstaller.cs Slic3rPostProcessingUploaderUnitTests/Services/Installer/OrcaFamily/OrcaFamilyProfileInstallerTests.cs
git commit -m "feat: implement Uninstall method for OrcaFamily profiles"
```

---

### Task 8: Concrete subclasses + SlicerInstallerRegistry

Wire up the three real OrcaSlicer-family slicers and a registry to manage them.

**Files:**
- Create: `Slic3rPostProcessingUploader/Services/Installer/OrcaFamily/OrcaSlicerInstaller.cs`
- Create: `Slic3rPostProcessingUploader/Services/Installer/OrcaFamily/SnapmakerOrcaInstaller.cs`
- Create: `Slic3rPostProcessingUploader/Services/Installer/OrcaFamily/AnycubicSlicerNextInstaller.cs`
- Create: `Slic3rPostProcessingUploader/Services/Installer/SlicerInstallerRegistry.cs`
- Create: `Slic3rPostProcessingUploaderUnitTests/Services/Installer/SlicerInstallerRegistryTests.cs`

No tests needed for the thin subclasses — they are a single string property, already covered by the base class tests. The registry gets a simple test.

**Step 1: Create the three subclasses**

`OrcaSlicerInstaller.cs`:
```csharp
namespace Slic3rPostProcessingUploader.Services.Installer.OrcaFamily
{
    internal class OrcaSlicerInstaller : OrcaFamilyProfileInstaller
    {
        public override string SlicerName => "OrcaSlicer";
        protected override string SlicerDirectoryName => "OrcaSlicer";
    }
}
```

`SnapmakerOrcaInstaller.cs`:
```csharp
namespace Slic3rPostProcessingUploader.Services.Installer.OrcaFamily
{
    internal class SnapmakerOrcaInstaller : OrcaFamilyProfileInstaller
    {
        public override string SlicerName => "Snapmaker Orca";
        protected override string SlicerDirectoryName => "Snapmaker_Orca";
    }
}
```

`AnycubicSlicerNextInstaller.cs`:
```csharp
namespace Slic3rPostProcessingUploader.Services.Installer.OrcaFamily
{
    internal class AnycubicSlicerNextInstaller : OrcaFamilyProfileInstaller
    {
        public override string SlicerName => "AnycubicSlicer Next";
        protected override string SlicerDirectoryName => "AnycubicSlicerNext";
    }
}
```

**Step 2: Create SlicerInstallerRegistry**

`SlicerInstallerRegistry.cs`:
```csharp
using Slic3rPostProcessingUploader.Services.Installer.OrcaFamily;

namespace Slic3rPostProcessingUploader.Services.Installer
{
    internal static class SlicerInstallerRegistry
    {
        /// <summary>
        /// All supported slicer installers. Add new slicers here.
        /// </summary>
        public static IReadOnlyList<ISlicerProfileInstaller> All =>
        [
            new OrcaSlicerInstaller(),
            new SnapmakerOrcaInstaller(),
            new AnycubicSlicerNextInstaller(),
        ];
    }
}
```

**Step 3: Write and run registry test**

Create `Slic3rPostProcessingUploaderUnitTests/Services/Installer/SlicerInstallerRegistryTests.cs`:

```csharp
using Slic3rPostProcessingUploader.Services.Installer;

namespace Slic3rPostProcessingUploaderUnitTests.Services.Installer
{
    [TestClass]
    public class SlicerInstallerRegistryTests
    {
        [TestMethod]
        public void All_ContainsThreeSlicers()
        {
            Assert.AreEqual(3, SlicerInstallerRegistry.All.Count);
        }

        [TestMethod]
        public void All_ContainsOrcaSlicer()
        {
            Assert.IsTrue(SlicerInstallerRegistry.All.Any(s => s.SlicerName == "OrcaSlicer"));
        }
    }
}
```

```bash
dotnet test --filter "FullyQualifiedName~SlicerInstallerRegistryTests" -v
```

Expected: PASS.

**Step 4: Commit**

```bash
git add Slic3rPostProcessingUploader/Services/Installer/ Slic3rPostProcessingUploaderUnitTests/Services/Installer/SlicerInstallerRegistryTests.cs
git commit -m "feat: add OrcaSlicer family subclasses and SlicerInstallerRegistry"
```

---

### Task 9: WizardService

The interactive terminal wizard that drives the install/uninstall flow.

**Files:**
- Create: `Slic3rPostProcessingUploader/Services/Installer/WizardService.cs`
- Create: `Slic3rPostProcessingUploaderUnitTests/Services/Installer/WizardServiceTests.cs`

The wizard has no complex logic of its own beyond I/O — it delegates to the installers. Test only the flag-building logic (which is testable without I/O).

**Step 1: Write tests for flag-building logic**

Create `Slic3rPostProcessingUploaderUnitTests/Services/Installer/WizardServiceTests.cs`:

```csharp
using Slic3rPostProcessingUploader.Services.Installer;

namespace Slic3rPostProcessingUploaderUnitTests.Services.Installer
{
    [TestClass]
    public class WizardServiceTests
    {
        [TestMethod]
        public void BuildFlags_DefaultTemplate_ReturnsDefaultFlag()
        {
            var flags = WizardService.BuildFlagsForTesting(useFullTemplate: false, optOutTelemetry: false, additionalFlags: "");
            Assert.AreEqual("--default", flags);
        }

        [TestMethod]
        public void BuildFlags_FullTemplate_ReturnsFullFlag()
        {
            var flags = WizardService.BuildFlagsForTesting(useFullTemplate: true, optOutTelemetry: false, additionalFlags: "");
            Assert.AreEqual("--full", flags);
        }

        [TestMethod]
        public void BuildFlags_WithTelemetryOptOut_AppendsTelemetryFlag()
        {
            var flags = WizardService.BuildFlagsForTesting(useFullTemplate: false, optOutTelemetry: true, additionalFlags: "");
            Assert.AreEqual("--default --opt-out-telemetry", flags);
        }

        [TestMethod]
        public void BuildFlags_WithAdditionalFlags_AppendsThemLast()
        {
            var flags = WizardService.BuildFlagsForTesting(useFullTemplate: true, optOutTelemetry: false, additionalFlags: "--local-dev");
            Assert.AreEqual("--full --local-dev", flags);
        }

        [TestMethod]
        public void BuildFlags_WithAllOptions_CombinesCorrectly()
        {
            var flags = WizardService.BuildFlagsForTesting(useFullTemplate: true, optOutTelemetry: true, additionalFlags: "--local-dev");
            Assert.AreEqual("--full --opt-out-telemetry --local-dev", flags);
        }
    }
}
```

**Step 2: Run tests to verify they fail**

```bash
dotnet test --filter "FullyQualifiedName~WizardServiceTests" -v
```

Expected: FAIL — `WizardService` doesn't exist.

**Step 3: Create WizardService**

Create `Slic3rPostProcessingUploader/Services/Installer/WizardService.cs`:

```csharp
namespace Slic3rPostProcessingUploader.Services.Installer
{
    internal class WizardService
    {
        private readonly IReadOnlyList<ISlicerProfileInstaller> _installers;
        private readonly string _executablePath;

        public WizardService(IReadOnlyList<ISlicerProfileInstaller> installers, string executablePath)
        {
            _installers = installers;
            _executablePath = executablePath;
        }

        public void RunInstall(bool dryRun)
        {
            PrintHeader();
            Console.WriteLine("Scanning for supported slicers...\n");

            var detected = _installers.Where(i => i.IsDetected()).ToList();

            if (detected.Count == 0)
            {
                Console.WriteLine("No supported slicers found on this machine.");
                Console.WriteLine("Press any key to exit.");
                Console.ReadKey();
                return;
            }

            // Show summary of all slicers
            foreach (var installer in _installers)
            {
                if (!installer.IsDetected())
                {
                    Console.WriteLine($"  {installer.SlicerName,-26} (not detected — skipped)");
                    continue;
                }

                var status = installer.GetInstallStatus(_executablePath);
                string statusText = status.IsInstalled
                    ? $"Already installed | {status.InstalledCount}/{status.ProfileCount} profiles | flags: {status.InstalledFlags}"
                    : $"Not installed | {status.ProfileCount} process profiles found";
                Console.WriteLine($"  Found: {installer.SlicerName,-20} {statusText}");
            }

            Console.WriteLine();

            // Per-slicer prompts
            bool anyInstalled = false;
            foreach (var installer in detected)
            {
                var status = installer.GetInstallStatus(_executablePath);

                Console.WriteLine($"--- {installer.SlicerName} ---");

                if (status.IsInstalled)
                {
                    Console.Write($"Already installed (flags: {status.InstalledFlags}). Reinstall with new flags? [y/N]: ");
                    var answer = Console.ReadLine()?.Trim().ToLower();
                    if (answer != "y") { Console.WriteLine(); continue; }
                }
                else
                {
                    Console.Write($"Install to {installer.SlicerName}? [Y/n]: ");
                    var answer = Console.ReadLine()?.Trim().ToLower();
                    if (answer == "n") { Console.WriteLine(); continue; }
                }

                string flags = PromptForFlags();
                var result = installer.Install(_executablePath, flags, dryRun);

                if (dryRun)
                    Console.WriteLine($"  [DRY RUN] Would create/update {result.Created + result.Updated} profiles ({result.Created} new, {result.Updated} updated, {result.Skipped} already up to date).");
                else
                    Console.WriteLine($"  Done: {result.Created} created, {result.Updated} updated, {result.Skipped} skipped.");

                if (result.WithOtherScripts > 0)
                    Console.WriteLine($"  Note: {result.WithOtherScripts} profile(s) had other post-process scripts — ours was appended alongside them.");

                anyInstalled = true;
                Console.WriteLine();
            }

            if (!anyInstalled)
                Console.WriteLine("No changes made.");
            else
                Console.WriteLine("Setup complete!");

            Console.WriteLine("\nPress any key to exit.");
            Console.ReadKey();
        }

        public void RunUninstall(bool dryRun)
        {
            PrintHeader();
            Console.WriteLine("Uninstall — scanning for installed profiles...\n");

            var detected = _installers.Where(i => i.IsDetected()).ToList();
            bool anyUninstalled = false;

            foreach (var installer in detected)
            {
                var status = installer.GetInstallStatus(_executablePath);
                if (!status.IsInstalled)
                {
                    Console.WriteLine($"  {installer.SlicerName}: not installed — skipped.");
                    continue;
                }

                Console.WriteLine($"--- {installer.SlicerName} ---");
                Console.Write($"Remove from {status.InstalledCount} profile(s)? [Y/n]: ");
                var answer = Console.ReadLine()?.Trim().ToLower();
                if (answer == "n") { Console.WriteLine(); continue; }

                var result = installer.Uninstall(_executablePath, dryRun);

                if (dryRun)
                    Console.WriteLine($"  [DRY RUN] Would remove from {result.RemovedFiles + result.ModifiedFiles} profile(s).");
                else
                    Console.WriteLine($"  Removed from {result.RemovedFiles + result.ModifiedFiles} profile(s). {result.ModifiedFiles} profile(s) had other settings — kept those files.");

                anyUninstalled = true;
                Console.WriteLine();
            }

            if (!anyUninstalled)
                Console.WriteLine("No changes made.");
            else
                Console.WriteLine("Uninstall complete!");

            Console.WriteLine("\nPress any key to exit.");
            Console.ReadKey();
        }

        private static string PromptForFlags()
        {
            Console.WriteLine();
            Console.WriteLine("  Note template:");
            Console.WriteLine("    1) Default (recommended)");
            Console.WriteLine("    2) Full");
            Console.Write("  Choice [1]: ");
            var templateChoice = Console.ReadLine()?.Trim();
            bool useFullTemplate = templateChoice == "2";

            Console.Write("  Opt out of telemetry? [y/N]: ");
            var telemetryAnswer = Console.ReadLine()?.Trim().ToLower();
            bool optOutTelemetry = telemetryAnswer == "y";

            Console.Write("  Additional flags (leave blank for none): ");
            var additionalFlags = Console.ReadLine()?.Trim() ?? "";

            Console.WriteLine();
            return BuildFlagsForTesting(useFullTemplate, optOutTelemetry, additionalFlags);
        }

        // Internal for testing
        internal static string BuildFlagsForTesting(bool useFullTemplate, bool optOutTelemetry, string additionalFlags)
        {
            var parts = new List<string>();
            parts.Add(useFullTemplate ? "--full" : "--default");
            if (optOutTelemetry) parts.Add("--opt-out-telemetry");
            if (!string.IsNullOrWhiteSpace(additionalFlags)) parts.Add(additionalFlags.Trim());
            return string.Join(" ", parts);
        }

        private static void PrintHeader()
        {
            Console.WriteLine("3D Print Log Uploader - Setup Wizard");
            Console.WriteLine("=====================================");
        }
    }
}
```

**Step 4: Run tests to verify they pass**

```bash
dotnet test --filter "FullyQualifiedName~WizardServiceTests" -v
```

Expected: All tests PASS.

**Step 5: Commit**

```bash
git add Slic3rPostProcessingUploader/Services/Installer/WizardService.cs Slic3rPostProcessingUploaderUnitTests/Services/Installer/WizardServiceTests.cs
git commit -m "feat: add WizardService with interactive install/uninstall terminal flow"
```

---

### Task 10: Wire into Program.cs

Hook the wizard and mode detection into the application entry point.

**Files:**
- Modify: `Slic3rPostProcessingUploader/Program.cs`

No new tests — this is wiring. The existing tests cover the components; the wizard flow is validated manually.

**Step 1: Add wizard launch logic**

In `Program.cs`, find the existing `if (arguments.DisplayHelp)` block (around line 40) and add the new mode handling **before** the `if (string.IsNullOrEmpty(arguments.InputFile))` check:

```csharp
// Handle install/uninstall wizard modes
if (arguments.Mode == AppMode.Wizard || arguments.Mode == AppMode.Install)
{
    string exePath = Environment.ProcessPath ?? System.Reflection.Assembly.GetExecutingAssembly().Location;
    var wizard = new WizardService(SlicerInstallerRegistry.All, exePath);
    wizard.RunInstall(arguments.IsDryRun);
    return;
}

if (arguments.Mode == AppMode.Uninstall)
{
    string exePath = Environment.ProcessPath ?? System.Reflection.Assembly.GetExecutingAssembly().Location;
    var wizard = new WizardService(SlicerInstallerRegistry.All, exePath);
    wizard.RunUninstall(arguments.IsDryRun);
    return;
}
```

Add the using statements at the top of `Program.cs`:
```csharp
using Slic3rPostProcessingUploader.Services.Installer;
```

**Step 2: Remove the old "no input file" exception**

The existing code throws if `InputFile` is null. This was correct before, but now `Wizard`/`Install`/`Uninstall` modes return before reaching that check. Verify the check still exists and is still correct for `PostProcess` mode — no change needed.

**Step 3: Build and verify**

```bash
dotnet build
```

Expected: Build succeeds with no errors.

**Step 4: Smoke test (manual)**

```bash
dotnet run --project Slic3rPostProcessingUploader -- install --dry-run
```

Expected: Wizard launches, scans for slicers, shows results, exits cleanly.

**Step 5: Run full test suite**

```bash
dotnet test
```

Expected: All tests PASS.

**Step 6: Commit**

```bash
git add Slic3rPostProcessingUploader/Program.cs
git commit -m "feat: wire wizard into Program.cs entry point for install/uninstall modes"
```

---

### Task 11: Update README and CLAUDE.md

Document the new CLI flags for users and future contributors.

**Files:**
- Modify: `README.md`
- Modify: `CLAUDE.md`

**Step 1: Update README**

Add a new section to `README.md` (after the existing "Options" section):

````markdown
## Setup Wizard

Instead of manually adding the uploader to each slicer profile, run the setup wizard:

**Windows:** Double-click `Slic3rPostProcessingUploader.exe`
**All platforms:** Run with no arguments:

```bash
Slic3rPostProcessingUploader.exe
# or
Slic3rPostProcessingUploader.exe install
```

The wizard will detect installed slicers, let you choose which to configure, and inject the uploader path into all process profiles automatically.

### Options

```bash
# Preview what would change without writing any files
Slic3rPostProcessingUploader.exe install --dry-run

# Remove the uploader from all profiles
Slic3rPostProcessingUploader.exe uninstall

# Preview uninstall
Slic3rPostProcessingUploader.exe uninstall --dry-run
```

### Supported Slicers (Setup Wizard)

| Slicer | Notes |
|--------|-------|
| OrcaSlicer | All process profiles |
| Snapmaker Orca | All process profiles |
| AnycubicSlicer Next | All process profiles, including cloud account profiles |
````

**Step 2: Update CLAUDE.md**

Add to the "Architecture" section of `CLAUDE.md`:

```markdown
### Installer / Wizard

- **AppMode enum**: Determines whether the app runs as post-processor, install wizard, or uninstall
- **Services/Installer/ISlicerProfileInstaller.cs**: Interface for each slicer family's installer
- **Services/Installer/OrcaFamily/**: Base class + subclasses for OrcaSlicer, Snapmaker_Orca, AnycubicSlicerNext
- **Services/Installer/SlicerInstallerRegistry.cs**: Registry of all supported installers (add new slicers here)
- **Services/Installer/WizardService.cs**: Interactive terminal wizard driving the install/uninstall flow

### Adding a New OrcaSlicer Fork

1. Create a subclass of `OrcaFamilyProfileInstaller` in `Services/Installer/OrcaFamily/`
2. Set `SlicerName` (display name) and `SlicerDirectoryName` (config folder name on disk)
3. Register it in `SlicerInstallerRegistry.All`
```

**Step 3: Commit**

```bash
git add README.md CLAUDE.md
git commit -m "docs: document setup wizard CLI and installer architecture"
```

---

## Summary

| Task | What it delivers |
|------|-----------------|
| 1 | AppMode enum + ArgumentParser install/uninstall mode detection |
| 2 | ISlicerProfileInstaller interface + result types |
| 3 | OrcaFamilyProfileInstaller config root + IsDetected |
| 4 | System profile discovery + user account dir enumeration |
| 5 | Install (JSON injection) logic |
| 6 | GetInstallStatus |
| 7 | Uninstall logic |
| 8 | Three concrete subclasses + SlicerInstallerRegistry |
| 9 | WizardService interactive terminal flow |
| 10 | Wire into Program.cs entry point |
| 11 | README + CLAUDE.md documentation |
