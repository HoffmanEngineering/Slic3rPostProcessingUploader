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

        [TestMethod]
        public void All_ContainsSnapmakerOrca()
        {
            Assert.IsTrue(SlicerInstallerRegistry.All.Any(s => s.SlicerName == "Snapmaker Orca"));
        }

        [TestMethod]
        public void All_ContainsAnycubicSlicerNext()
        {
            Assert.IsTrue(SlicerInstallerRegistry.All.Any(s => s.SlicerName == "AnycubicSlicer Next"));
        }
    }
}
