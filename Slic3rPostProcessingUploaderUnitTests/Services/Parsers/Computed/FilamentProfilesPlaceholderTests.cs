using Slic3rPostProcessingUploader.Services.Parsers;
using Slic3rPostProcessingUploader.Services.Parsers.Computed;

namespace Slic3rPostProcessingUploaderUnitTests.Services.Parsers.Computed
{
    /// <summary>
    /// Multi-filament exports repeat the filament profile once per slot ("A";"A";"A";"A";"A"). The placeholder
    /// collapses that into one entry per distinct profile with a count, which is what a person wants to read.
    /// </summary>
    [TestClass]
    public class FilamentProfilesPlaceholderTests
    {
        private static string Render(string gcode)
        {
            var settings = GcodeSettings.Parse(gcode, "=");
            return FilamentProfilesPlaceholder.Instance.Render(new ComputedContext(settings, ParseOptions.InMemory(gcode)));
        }

        [TestMethod]
        public void ShouldDescribeASingleFilament()
        {
            var note = Render("; filament_settings_id = \"Prusa Generic PLA @MK4S\"\n; filament_type = PLA\n; filament_vendor = Generic\n");

            Assert.AreEqual("Prusa Generic PLA @MK4S (PLA, Generic)", note);
        }

        [TestMethod]
        public void ShouldCollapseRepeatedProfilesIntoOneEntryWithACount()
        {
            var note = Render(
                "; filament_settings_id = \"Snapmaker PLA @U1\";\"Snapmaker PLA @U1\";\"Snapmaker PLA @U1\"\n" +
                "; filament_type = PLA;PLA;PLA\n" +
                "; filament_vendor = Snapmaker;Snapmaker;Snapmaker\n");

            Assert.AreEqual("Snapmaker PLA @U1 (PLA, Snapmaker) ×3", note);
        }

        [TestMethod]
        public void ShouldListDistinctProfilesInSlotOrder()
        {
            var note = Render(
                "; filament_settings_id = \"A\";\"B\";\"A\"\n" +
                "; filament_type = PLA;PETG;PLA\n" +
                "; filament_vendor = X;Y;X\n");

            Assert.AreEqual("A (PLA, X) ×2, B (PETG, Y)", note);
        }

        [TestMethod]
        public void ShouldOmitTypeAndVendorWhenTheyAreMissing()
        {
            Assert.AreEqual("A, B", Render("; filament_settings_id = A;B\n"));
            Assert.AreEqual("A (PLA)", Render("; filament_settings_id = A\n; filament_type = PLA\n"));
        }

        [TestMethod]
        public void ShouldRenderNothingWithoutFilamentProfiles()
        {
            Assert.AreEqual(string.Empty, Render("; layer_height = 0.2\n"));
        }
    }
}
