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
    }
}
