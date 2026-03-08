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
    }
}
