using Slic3rPostProcessingUploader.Services.Parsers;
using Slic3rPostProcessingUploader.Services.Parsers.OrcaSlicer;

namespace Slic3rPostProcessingUploaderUnitTests.Services.Parsers
{
    [TestClass]
    public class EstimateFilamentUsageInMgTests
    {
        private static double? Estimate(string gcode)
        {
            var settings = GcodeSettings.Parse(gcode, "=");
            return new OrcaParser("").EstimateFilamentUsageInMg(settings);
        }

        [TestMethod]
        public void ShouldUseFilamentDensityWhenPresentForPla()
        {
            var gcode = """
                ; filament_diameter = 1.75
                ; filament_density = 1.24
                ; filament_type = PLA
                ; filament used [mm] = 1000
                """;

            var result = Estimate(gcode);

            // Same formula the PLA table entry (1.24) would have produced.
            Assert.AreEqual(2982, result);
        }

        [TestMethod]
        public void ShouldUseFilamentDensityForMaterialNotInTable()
        {
            var gcode = """
                ; filament_diameter = 1.75
                ; filament_density = 1.21
                ; filament_type = TPU
                ; filament used [mm] = 1000
                """;

            var result = Estimate(gcode);

            Assert.IsTrue(result > 0, "Expected a non-zero weight estimate for a material not in the density table.");
        }

        [TestMethod]
        public void ShouldUseFirstValueFromCommaSeparatedDensityList()
        {
            // TPU is not in the material table, so this only passes if the first density value (1.24) is
            // actually used to compute the weight (1.26 would produce 3030, not 2982).
            var gcode = """
                ; filament_diameter = 1.75
                ; filament_density = 1.24,1.26
                ; filament_type = TPU
                ; filament used [mm] = 1000
                """;

            var result = Estimate(gcode);

            Assert.AreEqual(2982, result);
        }

        [TestMethod]
        public void ShouldFallBackToTableForPetgWhenDensityAbsent()
        {
            var gcode = """
                ; filament_diameter = 1.75
                ; filament_type = PETG
                ; filament used [mm] = 1000
                """;

            var result = Estimate(gcode);

            Assert.AreEqual(2958, result);
        }

        [TestMethod]
        public void ShouldReturnZeroForUnknownMaterialWhenDensityAbsent()
        {
            var gcode = """
                ; filament_diameter = 1.75
                ; filament_type = TPU
                ; filament used [mm] = 1000
                """;

            var result = Estimate(gcode);

            Assert.AreEqual(0d, result);
        }

        [TestMethod]
        public void ShouldPreferFilamentWeightInGramsOverDensity()
        {
            var gcode = """
                ; filament_diameter = 1.75
                ; filament_density = 1.24
                ; filament_type = PLA
                ; filament used [mm] = 1000
                ; filament used [g] = 5.00
                """;

            var result = Estimate(gcode);

            Assert.AreEqual(5000d, result);
        }
    }
}
