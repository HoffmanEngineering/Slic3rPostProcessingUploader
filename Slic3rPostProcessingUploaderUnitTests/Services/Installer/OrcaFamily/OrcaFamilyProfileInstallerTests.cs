using Slic3rPostProcessingUploader.Services.Installer.OrcaFamily;

namespace Slic3rPostProcessingUploaderUnitTests.Services.Installer.OrcaFamily
{
    [TestClass]
    public class OrcaFamilyProfileInstallerTests
    {
        // Concrete subclass for testing the abstract base
        private class TestOrcaInstaller : OrcaFamilyProfileInstaller
        {
            public TestOrcaInstaller(string? configRootOverride = null) : base(configRootOverride) { }
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
                existingPostProcess: "\"C:\\tools\\Slic3rPostProcessingUploader.exe\" --full");
            try
            {
                var installer = new TestOrcaInstaller(configRootOverride: root);
                var status = installer.GetInstallStatus("C:\\tools\\Slic3rPostProcessingUploader.exe");

                Assert.IsTrue(status.IsInstalled);
                Assert.AreEqual(1, status.ProfileCount);
                Assert.AreEqual(1, status.InstalledCount);
                Assert.AreEqual("--full", status.InstalledFlags);
            }
            finally { Directory.Delete(root, recursive: true); }
        }

        [TestMethod]
        public void GetInstallStatus_DetectsInstall_EvenWhenPathDiffersFromCurrent()
        {
            // Installed from path A, checking status with path B
            var root = BuildTempConfigDirWithUserOverride("0.20mm Standard @Printer",
                existingPostProcess: "\"C:\\old-path\\Slic3rPostProcessingUploader.exe\" --full");
            try
            {
                var installer = new TestOrcaInstaller(configRootOverride: root);
                // Checking with a completely different path
                var status = installer.GetInstallStatus("D:\\new-path\\Slic3rPostProcessingUploader.exe");

                Assert.IsTrue(status.IsInstalled);
                Assert.AreEqual(1, status.InstalledCount);
                Assert.AreEqual("--full", status.InstalledFlags);
            }
            finally { Directory.Delete(root, recursive: true); }
        }
    }
}
