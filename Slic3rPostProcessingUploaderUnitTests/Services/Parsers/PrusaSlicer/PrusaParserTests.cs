using Slic3rPostProcessingUploader.Services.Parsers.PrusaSlicer;
using Snapshooter.MSTest;

namespace Slic3rPostProcessingUploaderUnitTests.Services.Parsers.PrusaSlicer
{
    [TestClass]
    public class PrusaParserTests
    {
        [TestMethod]
        public void ShouldReturnAnEmptySettingWhenGcodeIsEmpty()
        {
            var parser = new PrusaParser("");
            var result = parser.ParseGcode("");
            Assert.IsNotNull(result);

            Snapshot.Match(result, matchOptions => matchOptions.HashField("settings.Snapshot"));
        }

        [TestMethod]
        public void ShouldDetectPrusaSlicer2And3Gcode()
        {
            Assert.IsTrue(PrusaParser.IsPrusaSlicer(PrusaParserTestGcode.Cube_PrusaSlicer2));
            Assert.IsTrue(PrusaParser.IsPrusaSlicer(PrusaParserTestGcode.Cube_PrusaSlicer3));
        }

        [TestMethod]
        public void ShouldReturnExpectedValuesWhenGivenPrusaSlicer2Gcode()
        {
            var parser = new PrusaParser("");
            var result = parser.ParseGcode(PrusaParserTestGcode.Cube_PrusaSlicer2);

            Assert.AreEqual("PrusaSlicer", result.Slicer);
            Assert.AreEqual("2.9.2", result.CuraVersion);
            Assert.AreEqual(10 * 60 + 42, result.settings.estimated_print_time_seconds);
            // filament_density is 0 in this profile, so the weight is estimated from 1332.37mm of 1.75mm PLA
            Assert.AreEqual(3973, result.settings.material_used_mg);

            Snapshot.Match(result, matchOptions => matchOptions.HashField("settings.Snapshot"));
        }

        [TestMethod]
        public void ShouldReturnExpectedValuesWhenGivenPrusaSlicer3Gcode()
        {
            var parser = new PrusaParser("");
            var result = parser.ParseGcode(PrusaParserTestGcode.Cube_PrusaSlicer3);

            Assert.AreEqual("PrusaSlicer", result.Slicer);
            Assert.AreEqual("3.0.0-alpha11", result.CuraVersion);
            Assert.AreEqual(26 * 60 + 41, result.settings.estimated_print_time_seconds);
            Assert.AreEqual(3850, result.settings.material_used_mg);

            Snapshot.Match(result, matchOptions => matchOptions.HashField("settings.Snapshot"));
        }

        [TestMethod]
        public void ShouldRenderEnumSupportMaterialFromPrusaSlicer3()
        {
            // PrusaSlicer 3.0 turned support_material from a 0/1 flag into an enum
            var parser = new PrusaParser("Generate Support: {{support_material}}");
            var result = parser.ParseGcode(PrusaParserTestGcode.Cube_PrusaSlicer3);

            Assert.AreEqual("Generate Support: enforcers_only", result.settings.note);
        }

        [DataTestMethod]
        [DataRow("Default", "PrusaSlicer2")]
        [DataRow("Default", "PrusaSlicer3")]
        [DataRow("Full", "PrusaSlicer2")]
        [DataRow("Full", "PrusaSlicer3")]
        public void EveryTemplatePlaceholderShouldResolveAgainstRealGcode(string templateName, string slicerVersion)
        {
            string template = templateName == "Default"
                ? new PrusaDefaultNoteTemplate().getNoteTemplate()
                : new PrusaFullNoteTemplate().getNoteTemplate();
            string gcode = slicerVersion == "PrusaSlicer2"
                ? PrusaParserTestGcode.Cube_PrusaSlicer2
                : PrusaParserTestGcode.Cube_PrusaSlicer3;

            var parser = new PrusaParser(template);
            var (numPlaceholders, numMatches) = parser.CountTemplateMatches(gcode);

            Assert.AreEqual(numPlaceholders, numMatches, $"{numPlaceholders - numMatches} placeholder(s) in the {templateName} template are not present in {slicerVersion} gcode");
        }
    }
}
