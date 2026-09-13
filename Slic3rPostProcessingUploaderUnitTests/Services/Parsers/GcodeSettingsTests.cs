using Slic3rPostProcessingUploader.Services.Parsers;

namespace Slic3rPostProcessingUploaderUnitTests.Services.Parsers
{
    [TestClass]
    public class GcodeSettingsTests
    {
        private static readonly char[] EqualsOnly = ['='];
        private static readonly char[] EqualsOrColon = ['=', ':'];

        [TestMethod]
        public void ShouldParseKeyValueCommentLines()
        {
            var settings = GcodeSettings.Parse("; layer_height = 0.2\n; wall_loops = 3\n", EqualsOnly);

            Assert.AreEqual("0.2", settings.Get("layer_height"));
            Assert.AreEqual("3", settings.Get("wall_loops"));
        }

        [TestMethod]
        public void ShouldReturnEmptyStringForUnknownKeys()
        {
            var settings = GcodeSettings.Parse("; layer_height = 0.2\n", EqualsOnly);

            Assert.AreEqual(string.Empty, settings.Get("nozzle_diameter"));
        }

        [TestMethod]
        public void ShouldIgnoreNonCommentLines()
        {
            var settings = GcodeSettings.Parse("G1 X = 5\nM117 layer_height = 9\n; layer_height = 0.2\n", EqualsOnly);

            Assert.AreEqual("0.2", settings.Get("layer_height"));
            Assert.AreEqual(string.Empty, settings.Get("X"));
        }

        [TestMethod]
        public void ShouldMatchKeysCaseInsensitively()
        {
            var settings = GcodeSettings.Parse("; Layer_Height = 0.2\n", EqualsOnly);

            Assert.AreEqual("0.2", settings.Get("layer_height"));
        }

        [TestMethod]
        public void ShouldTrimValuesAndHandleCarriageReturns()
        {
            var settings = GcodeSettings.Parse("; layer_height =  0.2 \r\n; filament_type = PLA\r\n", EqualsOnly);

            Assert.AreEqual("0.2", settings.Get("layer_height"));
            Assert.AreEqual("PLA", settings.Get("filament_type"));
        }

        [TestMethod]
        public void ShouldKeepTheFirstNonEmptyValueWhenAKeyRepeats()
        {
            var settings = GcodeSettings.Parse("; layer_height = \n; layer_height = 0.2\n; layer_height = 0.3\n", EqualsOnly);

            Assert.AreEqual("0.2", settings.Get("layer_height"));
        }

        [TestMethod]
        public void ShouldKeepEverythingAfterTheFirstSeparatorAsTheValue()
        {
            var settings = GcodeSettings.Parse("; start_gcode = M104 S[temp] ; a = b\n", EqualsOnly);

            Assert.AreEqual("M104 S[temp] ; a = b", settings.Get("start_gcode"));
        }

        [TestMethod]
        public void ShouldSupportKeysWithSpacesAndBrackets()
        {
            var settings = GcodeSettings.Parse("; filament used [mm] = 1234.5\n; estimated printing time (normal mode) = 21m 59s\n", EqualsOnly);

            Assert.AreEqual("1234.5", settings.Get("filament used [mm]"));
            Assert.AreEqual("21m 59s", settings.Get("estimated printing time (normal mode)"));
        }

        [TestMethod]
        public void ShouldRequireSpacesAroundTheSeparator()
        {
            // "; filament_diameter: 1.75" (no space before the colon) is a header-style line, not a setting, when only '=' is allowed.
            var settings = GcodeSettings.Parse("; filament_diameter: 1.75\n; filament_diameter = 1.75,1.75\n", EqualsOnly);

            Assert.AreEqual("1.75,1.75", settings.Get("filament_diameter"));
        }

        [TestMethod]
        public void ShouldSupportAlternateSeparators()
        {
            var settings = GcodeSettings.Parse("; total filament length [mm] : 1487.59\n; layer_height = 0.2\n", EqualsOrColon);

            Assert.AreEqual("1487.59", settings.Get("total filament length [mm]"));
            Assert.AreEqual("0.2", settings.Get("layer_height"));
        }

        [TestMethod]
        public void ShouldUseTheFirstSeparatorOnTheLine()
        {
            var settings = GcodeSettings.Parse("; note : a = b\n", EqualsOrColon);

            Assert.AreEqual("a = b", settings.Get("note"));
        }
    }
}
