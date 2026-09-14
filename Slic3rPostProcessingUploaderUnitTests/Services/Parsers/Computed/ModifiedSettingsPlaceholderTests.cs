using Slic3rPostProcessingUploader.Services.Parsers;
using Slic3rPostProcessingUploader.Services.Parsers.Computed;

namespace Slic3rPostProcessingUploaderUnitTests.Services.Parsers.Computed
{
    [TestClass]
    public class ModifiedSettingsPlaceholderTests
    {
        private static string Render(string gcode)
        {
            var settings = GcodeSettings.Parse(gcode, "=");
            return ModifiedSettingsPlaceholder.Instance.Render(new ComputedContext(settings, ParseOptions.InMemory(gcode)));
        }

        [TestMethod]
        public void ShouldListEachChangedKeyWithItsValueFromTheGcode()
        {
            var note = Render(
                "; different_settings_to_system = sparse_infill_density;wall_loops;;\n" +
                "; inherits_group = \"0.16 High Quality @U1\";;\n" +
                "; sparse_infill_density = 8%\n" +
                "; wall_loops = 3\n");

            Assert.AreEqual("Changed from \"0.16 High Quality @U1\": sparse_infill_density = 8%, wall_loops = 3", note);
        }

        [TestMethod]
        public void ShouldSayProfileWhenThereIsNoSystemParent()
        {
            var note = Render(
                "; different_settings_to_system = wall_loops\n" +
                "; inherits_group = ;;\n" +
                "; wall_loops = 3\n");

            Assert.AreEqual("Changed from profile: wall_loops = 3", note);
        }

        [TestMethod]
        public void ShouldSayProfileWhenInheritsGroupIsMissing()
        {
            var note = Render("; different_settings_to_system = wall_loops\n; wall_loops = 3\n");

            Assert.AreEqual("Changed from profile: wall_loops = 3", note);
        }

        [TestMethod]
        public void ShouldMarkKeysOrcaDoesNotEchoIntoTheGcode()
        {
            var note = Render("; different_settings_to_system = some_hidden_key\n; inherits_group = \"P\"\n");

            Assert.AreEqual("Changed from \"P\": some_hidden_key = (not in G-code)", note);
        }

        [TestMethod]
        public void ShouldRenderNothingWhenOnlyPostProcessChanged()
        {
            Assert.AreEqual(string.Empty, Render("; different_settings_to_system = post_process;;\n; inherits_group = \"P\";;\n"));
        }

        [TestMethod]
        public void ShouldRenderNothingWhenTheSettingIsAbsent()
        {
            Assert.AreEqual(string.Empty, Render("; layer_height = 0.2\n"));
        }
    }
}
