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

        [TestMethod]
        public void PresetConfigRoots_CoverEveryInstallerPlusBambuStudioAndPrusaSlicer()
        {
            // The parsers look up user presets under these; Bambu Studio and PrusaSlicer have no installer but keep
            // their config next to the Orca family's, so they are resolved the same way.
            var roots = SlicerInstallerRegistry.PresetConfigRoots().Select(Path.GetFileName).ToList();

            CollectionAssert.IsSubsetOf(SlicerInstallerRegistry.OrcaFamilyConfigRoots().Select(Path.GetFileName).ToList(), roots);
            CollectionAssert.Contains(roots, "BambuStudio");
            CollectionAssert.Contains(roots, "PrusaSlicer");
            // PrusaSlicer 2.x pre-releases keep their own data directory next to the stable one.
            CollectionAssert.Contains(roots, "PrusaSlicer-alpha");
            CollectionAssert.Contains(roots, "PrusaSlicer-beta");
            Assert.AreEqual(roots.Count, roots.Distinct().Count());
        }
    }
}
