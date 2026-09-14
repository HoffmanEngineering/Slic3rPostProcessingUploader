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
            Assert.IsNotNull(root);
            Assert.IsTrue(root.Contains("TestSlicer"));
            Assert.IsTrue(Path.IsPathRooted(root), root);
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

        private static void WriteVendorIndex(string root, string vendor, params string[] subPaths)
        {
            var list = string.Join(",", subPaths.Select(p => $$"""{"name": "{{Path.GetFileNameWithoutExtension(p)}}", "sub_path": "{{p}}"}"""));
            File.WriteAllText(
                Path.Combine(root, "system", $"{vendor}.json"),
                $$"""{"name": "{{vendor}}", "version": "1.0.0", "process_list": [{{list}}], "filament_list": [], "machine_list": []}""");
        }

        [TestMethod]
        public void FindSelectableSystemProfiles_WhenVendorIndexExists_IgnoresProcessFilesNotListedInIt()
        {
            // Real vendor folders accumulate leftovers ("… copy.json", "…_old.json") that carry the same
            // "name" as the live profile but are absent from the vendor index, so the slicer never shows them.
            var root = BuildTempConfigDir(
                ("0.20mm Standard @Printer", true),
                ("0.20mm Standard @Printer copy", true),
                ("0.10mm Fine @Printer", true)
            );
            // The "copy" file claims the live profile's name.
            File.WriteAllText(
                Path.Combine(root, "system", "Vendor", "process", "0.20mm Standard @Printer copy.json"),
                """{"name": "0.20mm Standard @Printer", "instantiation": "true", "from": "system", "version": "1.0.0"}""");
            WriteVendorIndex(root, "Vendor", "process/0.20mm Standard @Printer.json", "process/0.10mm Fine @Printer.json");

            try
            {
                var installer = new TestOrcaInstaller(configRootOverride: root);
                var profiles = installer.FindSelectableSystemProfilesForTesting();

                CollectionAssert.AreEquivalent(
                    new[] { "0.20mm Standard @Printer", "0.10mm Fine @Printer" },
                    profiles.Select(p => p.Name).ToArray());
                StringAssert.EndsWith(profiles.Single(p => p.Name == "0.20mm Standard @Printer").FilePath, "0.20mm Standard @Printer.json");
            }
            finally
            {
                Directory.Delete(root, recursive: true);
            }
        }

        [TestMethod]
        public void FindSelectableSystemProfiles_WhenVendorIndexListsMissingFile_SkipsIt()
        {
            var root = BuildTempConfigDir(("0.20mm Standard @Printer", true));
            WriteVendorIndex(root, "Vendor", "process/0.20mm Standard @Printer.json", "process/0.30mm Removed @Printer.json");

            try
            {
                var profiles = new TestOrcaInstaller(configRootOverride: root).FindSelectableSystemProfilesForTesting();
                CollectionAssert.AreEqual(new[] { "0.20mm Standard @Printer" }, profiles.Select(p => p.Name).ToArray());
            }
            finally
            {
                Directory.Delete(root, recursive: true);
            }
        }

        [TestMethod]
        public void FindSelectableSystemProfiles_WithoutVendorIndex_DeduplicatesByName()
        {
            var root = BuildTempConfigDir(("0.20mm Standard @Printer", true));
            File.WriteAllText(
                Path.Combine(root, "system", "Vendor", "process", "0.20mm Standard @Printer_old.json"),
                """{"name": "0.20mm Standard @Printer", "instantiation": "true", "from": "system", "version": "1.0.0"}""");

            try
            {
                var profiles = new TestOrcaInstaller(configRootOverride: root).FindSelectableSystemProfilesForTesting();
                Assert.AreEqual(1, profiles.Count);
            }
            finally
            {
                Directory.Delete(root, recursive: true);
            }
        }

        [TestMethod]
        public void FindSelectableSystemProfiles_ToleratesDuplicateJsonKeys()
        {
            // Anycubic ships "0.20mm High Quality @Anycubic Kobra S1 0.4 nozzle.json" with "is_custom_defined" twice;
            // the slicer loads it, so the installer must too.
            var root = BuildTempConfigDir(("0.10mm Fine @Printer", true));
            File.WriteAllText(
                Path.Combine(root, "system", "Vendor", "process", "0.20mm Standard @Printer.json"),
                """{"name": "0.20mm Standard @Printer", "is_custom_defined": "0", "instantiation": "true", "is_custom_defined": "0", "version": "1.0.0"}""");

            try
            {
                var profiles = new TestOrcaInstaller(configRootOverride: root).FindSelectableSystemProfilesForTesting();
                CollectionAssert.AreEquivalent(
                    new[] { "0.10mm Fine @Printer", "0.20mm Standard @Printer" },
                    profiles.Select(p => p.Name).ToArray());
            }
            finally
            {
                Directory.Delete(root, recursive: true);
            }
        }

        [TestMethod]
        public void FindSelectableSystemProfiles_SkipsMalformedJson()
        {
            var root = BuildTempConfigDir(("0.10mm Fine @Printer", true));
            File.WriteAllText(Path.Combine(root, "system", "Vendor", "process", "broken.json"), "{ not json");

            try
            {
                var profiles = new TestOrcaInstaller(configRootOverride: root).FindSelectableSystemProfilesForTesting();
                CollectionAssert.AreEqual(new[] { "0.10mm Fine @Printer" }, profiles.Select(p => p.Name).ToArray());
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
            Dictionary<string, string>? extraFields = null,
            bool ownedBySuffix = false)
        {
            string overrideName = ownedBySuffix ? profileName + " - 3DPrintLog" : profileName;
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
                    ["name"] = overrideName,
                    ["print_settings_id"] = overrideName,
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
                    Path.Combine(userProcessDir, $"{overrideName}.json"),
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

                var overridePath = Path.Combine(root, "user", "default", "process", "0.20mm Standard @Printer - 3DPrintLog.json");
                Assert.IsTrue(File.Exists(overridePath));

                var node = System.Text.Json.Nodes.JsonNode.Parse(File.ReadAllText(overridePath));
                var postProcess = node!["post_process"]!.AsArray();
                Assert.AreEqual(1, postProcess.Count);
                Assert.IsTrue(postProcess[0]!.GetValue<string>().Contains("uploader.exe"));
                Assert.AreEqual("0.20mm Standard @Printer - 3DPrintLog", node!["name"]!.GetValue<string>());
                Assert.AreEqual("0.20mm Standard @Printer", node!["inherits"]!.GetValue<string>());
            }
            finally { Directory.Delete(root, recursive: true); }
        }

        [TestMethod]
        public void Install_UpdatesUserOverride_WhenOverrideExistsWithoutOurScript()
        {
            var root = BuildTempConfigDirWithUserOverride("0.20mm Standard @Printer");
            // Manually create the suffixed file with the other script
            var suffixedPath = Path.Combine(root, "user", "default", "process", "0.20mm Standard @Printer - 3DPrintLog.json");
            var existingNode = new System.Text.Json.Nodes.JsonObject
            {
                ["from"] = "User",
                ["inherits"] = "0.20mm Standard @Printer",
                ["name"] = "0.20mm Standard @Printer - 3DPrintLog",
                ["post_process"] = new System.Text.Json.Nodes.JsonArray(
                    System.Text.Json.Nodes.JsonValue.Create("C:\\other-script.exe")),
                ["print_settings_id"] = "0.20mm Standard @Printer - 3DPrintLog",
                ["version"] = "2.0.0"
            };
            File.WriteAllText(suffixedPath, existingNode.ToJsonString());
            try
            {
                var installer = new TestOrcaInstaller(configRootOverride: root);
                var result = installer.Install("C:\\uploader.exe", "--full", dryRun: false);

                Assert.AreEqual(0, result.Created);
                Assert.AreEqual(1, result.Updated);
                Assert.AreEqual(1, result.WithOtherScripts);

                var overridePath = Path.Combine(root, "user", "default", "process", "0.20mm Standard @Printer - 3DPrintLog.json");
                var node = System.Text.Json.Nodes.JsonNode.Parse(File.ReadAllText(overridePath));
                var postProcess = node!["post_process"]!.AsArray();
                Assert.AreEqual(2, postProcess.Count); // other script + ours
            }
            finally { Directory.Delete(root, recursive: true); }
        }

        [TestMethod]
        public void Install_SkipsProfile_WhenOurScriptAlreadyPresent()
        {
            var root = BuildTempConfigDirWithUserOverride("0.20mm Standard @Printer");
            // Manually create the suffixed file with our script already present
            var suffixedPath = Path.Combine(root, "user", "default", "process", "0.20mm Standard @Printer - 3DPrintLog.json");
            var existingNode = new System.Text.Json.Nodes.JsonObject
            {
                ["from"] = "User",
                ["inherits"] = "0.20mm Standard @Printer",
                ["name"] = "0.20mm Standard @Printer - 3DPrintLog",
                ["post_process"] = new System.Text.Json.Nodes.JsonArray(
                    System.Text.Json.Nodes.JsonValue.Create("C:\\Slic3rPostProcessingUploader.exe --full")),
                ["print_settings_id"] = "0.20mm Standard @Printer - 3DPrintLog",
                ["version"] = "2.0.0"
            };
            File.WriteAllText(suffixedPath, existingNode.ToJsonString());
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

                var overridePath = Path.Combine(root, "user", "default", "process", "0.20mm Standard @Printer - 3DPrintLog.json");
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

                var overridePath = Path.Combine(root, "user", "default", "process", "0.20mm Standard @Printer - 3DPrintLog.json");
                var node = System.Text.Json.Nodes.JsonNode.Parse(File.ReadAllText(overridePath));
                var entry = node!["post_process"]!.AsArray()[0]!.GetValue<string>();
                Assert.IsTrue(entry.StartsWith("\"C:\\My Tools\\uploader.exe\""));
            }
            finally { Directory.Delete(root, recursive: true); }
        }

        [TestMethod]
        public void Install_WritesQuotesAndNonAsciiLiterally_NotAsUnicodeEscapes()
        {
            // The files are user-visible and the slicer writes plain UTF-8, so a quoted path must appear as
            // \"C:\\...\" rather than \u0022C:\\...\u0022, and an accented user name must not become \u00F6.
            var root = BuildTempConfigDirWithUserOverride("0.20mm Standard @Printer");
            try
            {
                var installer = new TestOrcaInstaller(configRootOverride: root);
                installer.Install("C:\\Users\\Jörg\\3D Print Log\\uploader.exe", "--full", dryRun: false);

                var overridePath = Path.Combine(root, "user", "default", "process", "0.20mm Standard @Printer - 3DPrintLog.json");
                var text = File.ReadAllText(overridePath);
                StringAssert.Contains(text, "\"\\\"C:\\\\Users\\\\Jörg\\\\3D Print Log\\\\uploader.exe\\\" --full\"");
                Assert.IsFalse(text.Contains("\\u00"), text);
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
            var root = BuildTempConfigDirWithUserOverride("0.20mm Standard @Printer");
            var suffixedPath = Path.Combine(root, "user", "default", "process", "0.20mm Standard @Printer - 3DPrintLog.json");
            File.WriteAllText(suffixedPath, """{"from":"User","inherits":"0.20mm Standard @Printer","name":"0.20mm Standard @Printer - 3DPrintLog","post_process":["\"C:\\tools\\Slic3rPostProcessingUploader.exe\" --full"],"print_settings_id":"0.20mm Standard @Printer - 3DPrintLog","version":"2.0.0"}""");
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
            var root = BuildTempConfigDirWithUserOverride("0.20mm Standard @Printer");
            var suffixedPath = Path.Combine(root, "user", "default", "process", "0.20mm Standard @Printer - 3DPrintLog.json");
            File.WriteAllText(suffixedPath, """{"from":"User","inherits":"0.20mm Standard @Printer","name":"0.20mm Standard @Printer - 3DPrintLog","post_process":["\"C:\\old-path\\Slic3rPostProcessingUploader.exe\" --full"],"print_settings_id":"0.20mm Standard @Printer - 3DPrintLog","version":"2.0.0"}""");
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

        [TestMethod]
        public void Uninstall_DeletesFile_WhenOverrideHasNoOtherCustomizations()
        {
            var root = BuildTempConfigDirWithUserOverride("0.20mm Standard @Printer", ownedBySuffix: true,
                existingPostProcess: "\"C:\\Slic3rPostProcessingUploader.exe\" --full");
            var overridePath = Path.Combine(root, "user", "default", "process", "0.20mm Standard @Printer - 3DPrintLog.json");

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
            var root = BuildTempConfigDirWithUserOverride("0.20mm Standard @Printer", ownedBySuffix: true,
                existingPostProcess: "\"C:\\Slic3rPostProcessingUploader.exe\" --full",
                extraFields: new Dictionary<string, string> { ["wall_loops"] = "3" });
            var overridePath = Path.Combine(root, "user", "default", "process", "0.20mm Standard @Printer - 3DPrintLog.json");

            try
            {
                var installer = new TestOrcaInstaller(configRootOverride: root);
                var result = installer.Uninstall("C:\\Slic3rPostProcessingUploader.exe", dryRun: false);

                Assert.IsTrue(File.Exists(overridePath));
                Assert.AreEqual(0, result.RemovedFiles);
                Assert.AreEqual(1, result.ModifiedFiles);

                // Our entry should be gone, post_process key removed entirely
                var node = System.Text.Json.Nodes.JsonNode.Parse(File.ReadAllText(overridePath));
                Assert.IsNull(node?["post_process"]);
            }
            finally { Directory.Delete(root, recursive: true); }
        }

        [TestMethod]
        public void Uninstall_LeavesOtherScripts_WhenProfileHasMultiple()
        {
            var root = BuildTempConfigDirWithUserOverride("0.20mm Standard @Printer", ownedBySuffix: true,
                existingPostProcess: "C:\\other-script.exe");

            // Manually add our script alongside the other
            var overridePath = Path.Combine(root, "user", "default", "process", "0.20mm Standard @Printer - 3DPrintLog.json");
            var node = System.Text.Json.Nodes.JsonNode.Parse(File.ReadAllText(overridePath))!;
            node["post_process"]!.AsArray().Add(System.Text.Json.Nodes.JsonValue.Create("\"C:\\Slic3rPostProcessingUploader.exe\" --full"));
            File.WriteAllText(overridePath, node.ToJsonString());

            try
            {
                var installer = new TestOrcaInstaller(configRootOverride: root);
                installer.Uninstall("C:\\Slic3rPostProcessingUploader.exe", dryRun: false);

                var result_node = System.Text.Json.Nodes.JsonNode.Parse(File.ReadAllText(overridePath));
                var postProcess = result_node!["post_process"]!.AsArray();
                Assert.AreEqual(1, postProcess.Count);
                Assert.IsTrue(postProcess[0]!.GetValue<string>().Contains("other-script.exe"));
            }
            finally { Directory.Delete(root, recursive: true); }
        }

        [TestMethod]
        public void Uninstall_DryRun_WritesNoChanges()
        {
            var root = BuildTempConfigDirWithUserOverride("0.20mm Standard @Printer", ownedBySuffix: true,
                existingPostProcess: "\"C:\\Slic3rPostProcessingUploader.exe\" --full");
            var overridePath = Path.Combine(root, "user", "default", "process", "0.20mm Standard @Printer - 3DPrintLog.json");
            var originalContent = File.ReadAllText(overridePath);

            try
            {
                var installer = new TestOrcaInstaller(configRootOverride: root);
                var result = installer.Uninstall("C:\\Slic3rPostProcessingUploader.exe", dryRun: true);

                Assert.AreEqual(originalContent, File.ReadAllText(overridePath));
                // Counters still reflect what would have happened
                Assert.AreEqual(1, result.RemovedFiles);
            }
            finally { Directory.Delete(root, recursive: true); }
        }
        private static System.Text.Json.Nodes.JsonArray ReadPostProcess(string path) =>
            System.Text.Json.Nodes.JsonNode.Parse(File.ReadAllText(path))!["post_process"]!.AsArray();

        // ---- A: a hand-made profile that already runs the uploader covers its parent system profile ----

        [TestMethod]
        public void Install_SkipsSystemProfile_WhenHandMadeChildAlreadyRunsUploader()
        {
            // The hand-made file is "0.20mm Standard @Printer.json" (no suffix) and inherits the system profile.
            var root = BuildTempConfigDirWithUserOverride("0.20mm Standard @Printer",
                existingPostProcess: "\"C:\\uploader\\Slic3rPostProcessingUploader.exe\" --full");
            try
            {
                var result = new TestOrcaInstaller(configRootOverride: root)
                    .Install("C:\\uploader\\Slic3rPostProcessingUploader.exe", "--default", dryRun: false);

                Assert.AreEqual(0, result.Created);
                Assert.AreEqual(1, result.CoveredByHandMade);
                Assert.IsFalse(File.Exists(Path.Combine(root, "user", "default", "process", "0.20mm Standard @Printer - 3DPrintLog.json")));
            }
            finally { Directory.Delete(root, recursive: true); }
        }

        [TestMethod]
        public void Install_IgnoresHandMadeChild_WhenItDoesNotRunUploader()
        {
            var root = BuildTempConfigDirWithUserOverride("0.20mm Standard @Printer",
                existingPostProcess: "C:\\other-script.exe");
            try
            {
                var result = new TestOrcaInstaller(configRootOverride: root)
                    .Install("C:\\uploader\\Slic3rPostProcessingUploader.exe", "--full", dryRun: false);

                Assert.AreEqual(1, result.Created);
                Assert.AreEqual(0, result.CoveredByHandMade);
                Assert.IsTrue(File.Exists(Path.Combine(root, "user", "default", "process", "0.20mm Standard @Printer - 3DPrintLog.json")));
            }
            finally { Directory.Delete(root, recursive: true); }
        }

        // ---- C: stale uploader paths are refreshed ----

        [TestMethod]
        public void Install_ReplacesEntry_WhenOwnedOverrideHasStalePathOrFlags()
        {
            var root = BuildTempConfigDirWithUserOverride("0.20mm Standard @Printer", ownedBySuffix: true,
                existingPostProcess: "\"C:\\old\\Slic3rPostProcessingUploader.exe\" --default");
            var overridePath = Path.Combine(root, "user", "default", "process", "0.20mm Standard @Printer - 3DPrintLog.json");
            try
            {
                var result = new TestOrcaInstaller(configRootOverride: root)
                    .Install("C:\\new\\Slic3rPostProcessingUploader.exe", "--full", dryRun: false);

                Assert.AreEqual(1, result.Updated);
                Assert.AreEqual(0, result.Skipped);
                CollectionAssert.AreEqual(
                    new[] { "C:\\new\\Slic3rPostProcessingUploader.exe --full" },
                    ReadPostProcess(overridePath).Select(e => e!.GetValue<string>()).ToArray());
            }
            finally { Directory.Delete(root, recursive: true); }
        }

        [TestMethod]
        public void Install_RefreshesPathButKeepsFlags_WhenHandMadeProfilePointsAtOldExe()
        {
            var root = BuildTempConfigDirWithUserOverride("0.20mm Standard @Printer",
                existingPostProcess: "\"C:\\old\\Slic3rPostProcessingUploader.exe\" --default --opt-out-telemetry",
                extraFields: new Dictionary<string, string> { ["wall_loops"] = "3" });
            var handMadePath = Path.Combine(root, "user", "default", "process", "0.20mm Standard @Printer.json");
            try
            {
                var result = new TestOrcaInstaller(configRootOverride: root)
                    .Install("C:\\new dir\\Slic3rPostProcessingUploader.exe", "--full", dryRun: false);

                Assert.AreEqual(1, result.CoveredByHandMade);
                Assert.AreEqual(1, result.HandMadeRefreshed);
                var node = System.Text.Json.Nodes.JsonNode.Parse(File.ReadAllText(handMadePath))!;
                CollectionAssert.AreEqual(
                    new[] { "\"C:\\new dir\\Slic3rPostProcessingUploader.exe\" --default --opt-out-telemetry" },
                    node["post_process"]!.AsArray().Select(e => e!.GetValue<string>()).ToArray());
                Assert.AreEqual("3", node["wall_loops"]!.GetValue<string>(), "other settings must survive");
            }
            finally { Directory.Delete(root, recursive: true); }
        }

        [TestMethod]
        public void Install_LeavesHandMadeProfileUntouched_WhenItsPathIsAlreadyCurrent()
        {
            var root = BuildTempConfigDirWithUserOverride("0.20mm Standard @Printer",
                existingPostProcess: "\"C:\\uploader\\Slic3rPostProcessingUploader.exe\" --default");
            var handMadePath = Path.Combine(root, "user", "default", "process", "0.20mm Standard @Printer.json");
            var original = File.ReadAllText(handMadePath);
            try
            {
                var result = new TestOrcaInstaller(configRootOverride: root)
                    .Install("C:\\uploader\\Slic3rPostProcessingUploader.exe", "--full", dryRun: false);

                Assert.AreEqual(1, result.CoveredByHandMade);
                Assert.AreEqual(0, result.HandMadeRefreshed);
                Assert.AreEqual(original, File.ReadAllText(handMadePath));
            }
            finally { Directory.Delete(root, recursive: true); }
        }

        [TestMethod]
        public void Install_DryRun_ReportsHandMadeRefreshWithoutWriting()
        {
            var root = BuildTempConfigDirWithUserOverride("0.20mm Standard @Printer",
                existingPostProcess: "\"C:\\old\\Slic3rPostProcessingUploader.exe\" --default");
            var handMadePath = Path.Combine(root, "user", "default", "process", "0.20mm Standard @Printer.json");
            var original = File.ReadAllText(handMadePath);
            try
            {
                var result = new TestOrcaInstaller(configRootOverride: root)
                    .Install("C:\\new\\Slic3rPostProcessingUploader.exe", "--full", dryRun: true);

                Assert.AreEqual(1, result.HandMadeRefreshed);
                Assert.AreEqual(original, File.ReadAllText(handMadePath));
            }
            finally { Directory.Delete(root, recursive: true); }
        }

        [TestMethod]
        public void Install_SkipsUnreadableUserOverride_AndStillInstallsTheRest()
        {
            var root = BuildTempConfigDir(("0.20mm Standard @Printer", true), ("0.10mm Fine @Printer", true));
            File.WriteAllText(Path.Combine(root, "user", "default", "process", "0.20mm Standard @Printer - 3DPrintLog.json"), "{ not json");
            try
            {
                var result = new TestOrcaInstaller(configRootOverride: root)
                    .Install("C:\\uploader\\Slic3rPostProcessingUploader.exe", "--full", dryRun: false);

                Assert.AreEqual(1, result.Created);
                Assert.AreEqual(1, result.Unreadable);
                Assert.IsTrue(File.Exists(Path.Combine(root, "user", "default", "process", "0.10mm Fine @Printer - 3DPrintLog.json")));
            }
            finally { Directory.Delete(root, recursive: true); }
        }

        // ---- B: uninstall only touches installer-owned overrides ----

        [TestMethod]
        public void Uninstall_LeavesHandMadeProfileAlone_ButReportsIt()
        {
            var root = BuildTempConfigDirWithUserOverride("0.20mm Standard @Printer",
                existingPostProcess: "\"C:\\Slic3rPostProcessingUploader.exe\" --full");
            var handMadePath = Path.Combine(root, "user", "default", "process", "0.20mm Standard @Printer.json");
            var original = File.ReadAllText(handMadePath);
            try
            {
                var result = new TestOrcaInstaller(configRootOverride: root).Uninstall("C:\\Slic3rPostProcessingUploader.exe", dryRun: false);

                Assert.AreEqual(0, result.RemovedFiles + result.ModifiedFiles);
                Assert.AreEqual(1, result.HandMadeLeft);
                Assert.AreEqual(original, File.ReadAllText(handMadePath));
            }
            finally { Directory.Delete(root, recursive: true); }
        }

        [TestMethod]
        public void Uninstall_RemovesOrphanedOwnedOverride_WhoseSystemProfileIsGone()
        {
            var root = BuildTempConfigDir(("0.10mm Fine @Printer", true));
            var orphanPath = Path.Combine(root, "user", "default", "process", "0.20mm Standard @Gone - 3DPrintLog.json");
            File.WriteAllText(orphanPath, """{"from": "User", "inherits": "0.20mm Standard @Gone", "name": "0.20mm Standard @Gone - 3DPrintLog", "post_process": ["C:\\Slic3rPostProcessingUploader.exe"], "print_settings_id": "0.20mm Standard @Gone - 3DPrintLog", "version": "2.0.0"}""");
            try
            {
                var result = new TestOrcaInstaller(configRootOverride: root).Uninstall("C:\\Slic3rPostProcessingUploader.exe", dryRun: false);

                Assert.AreEqual(1, result.RemovedFiles);
                Assert.IsFalse(File.Exists(orphanPath));
            }
            finally { Directory.Delete(root, recursive: true); }
        }
        // ---- the executable may have been renamed: our own path still identifies our entry ----

        [TestMethod]
        public void Install_SkipsOwnedOverride_WhenExecutableIsRenamed()
        {
            var root = BuildTempConfigDirWithUserOverride("0.20mm Standard @Printer", ownedBySuffix: true,
                existingPostProcess: "/opt/uploader --full");
            var overridePath = Path.Combine(root, "user", "default", "process", "0.20mm Standard @Printer - 3DPrintLog.json");
            try
            {
                var result = new TestOrcaInstaller(configRootOverride: root).Install("/opt/uploader", "--full", dryRun: false);

                Assert.AreEqual(1, result.Skipped);
                Assert.AreEqual(0, result.Updated);
                Assert.AreEqual(1, ReadPostProcess(overridePath).Count, "entry must not be duplicated");
            }
            finally { Directory.Delete(root, recursive: true); }
        }

        [TestMethod]
        public void Uninstall_RemovesOwnedOverride_WhenExecutableIsRenamed()
        {
            var root = BuildTempConfigDirWithUserOverride("0.20mm Standard @Printer", ownedBySuffix: true,
                existingPostProcess: "\"C:\\Tools\\uploader.exe\" --full");
            var overridePath = Path.Combine(root, "user", "default", "process", "0.20mm Standard @Printer - 3DPrintLog.json");
            try
            {
                var result = new TestOrcaInstaller(configRootOverride: root).Uninstall("C:\\Tools\\uploader.exe", dryRun: false);

                Assert.AreEqual(1, result.RemovedFiles);
                Assert.IsFalse(File.Exists(overridePath));
            }
            finally { Directory.Delete(root, recursive: true); }
        }

        [TestMethod]
        public void GetInstallStatus_CountsOwnedOverride_WhenExecutableIsRenamed()
        {
            var root = BuildTempConfigDirWithUserOverride("0.20mm Standard @Printer", ownedBySuffix: true,
                existingPostProcess: "/opt/uploader --default");
            try
            {
                var status = new TestOrcaInstaller(configRootOverride: root).GetInstallStatus("/opt/uploader");

                Assert.IsTrue(status.IsInstalled);
                Assert.AreEqual("--default", status.InstalledFlags);
            }
            finally { Directory.Delete(root, recursive: true); }
        }
    }
}
