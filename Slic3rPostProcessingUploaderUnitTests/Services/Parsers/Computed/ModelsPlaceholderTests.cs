using Slic3rPostProcessingUploader.Services.Parsers;
using Slic3rPostProcessingUploader.Services.Parsers.Computed;

namespace Slic3rPostProcessingUploaderUnitTests.Services.Parsers.Computed
{
    [TestClass]
    public class ModelsPlaceholderTests
    {
        private const string Header = "; outer_wall_line_width = 0.4\n; line_width = 0.45\n";

        private static string Instance(string name, int id, string body) =>
            $"; printing object {name} id:{id} copy 0\n{body}; stop printing object {name} id:{id} copy 0\n";

        private static string Box(double x, double y, double w, double d) =>
            $";TYPE:Outer wall\nG1 X{x} Y{y} E0.5\nG1 X{x + w} Y{y + d} E0.5\n";

        private static string Render(string gcode, Action<string>? log = null, Func<Stream>? opener = null)
        {
            var settings = GcodeSettings.Parse(gcode, "=");
            var options = new ParseOptions(opener ?? ParseOptions.InMemory(gcode).OpenFullGcode, log ?? (_ => { }));
            return ModelsPlaceholder.Instance.Render(new ComputedContext(settings, options));
        }

        [TestMethod]
        public void ShouldListEachObjectWithOuterDimensionsUnderItsOwnHeading()
        {
            var gcode = Header + ";Z:0.2\n" + Instance("Cube", 0, Box(10, 10, 20, 20)) + ";Z:26.9\n" + Instance("Cube", 0, Box(10, 10, 20, 20));

            // 20 mm between centrelines plus one 0.4 mm outer wall line width. The value carries its heading and a
            // trailing newline so the template can drop the whole section when there is nothing to show.
            Assert.AreEqual("Models:\n  Cube  ×1   20.4 × 20.4 × 26.9 mm\n", Render(gcode));
        }

        [TestMethod]
        public void ShouldGroupCopiesWithTheSameNameAndSize()
        {
            var gcode = Header + ";Z:5\n" + Instance("Cube", 0, Box(10, 10, 20, 20)) + Instance("Cube", 1, Box(50, 50, 20, 20)) + Instance("Cube", 2, Box(90, 90, 20, 20));

            Assert.AreEqual("Models:\n  Cube  ×3   20.4 × 20.4 × 5.0 mm\n", Render(gcode));
        }

        [TestMethod]
        public void ShouldSeparateSameNamedObjectsOfDifferentSizes()
        {
            var gcode = Header + ";Z:5\n" + Instance("Cube", 0, Box(10, 10, 20, 20)) + Instance("Cube", 1, Box(50, 50, 30, 20));

            Assert.AreEqual("Models:\n  Cube  ×1   20.4 × 20.4 × 5.0 mm\n  Cube  ×1   30.4 × 20.4 × 5.0 mm\n", Render(gcode));
        }

        [TestMethod]
        public void ShouldPadNamesToTheLongestAndOrderByFirstAppearance()
        {
            var gcode = Header + ";Z:5\n" + Instance("3DBenchy.drc", 0, Box(0, 0, 60, 31)) + Instance("Cube", 1, Box(100, 100, 20, 20)) + Instance("3DBenchy.drc", 2, Box(0, 50, 60, 31));

            Assert.AreEqual(
                "Models:\n" +
                "  3DBenchy.drc  ×2   60.4 × 31.4 × 5.0 mm\n" +
                "  Cube          ×1   20.4 × 20.4 × 5.0 mm\n",
                Render(gcode));
        }

        [TestMethod]
        public void ShouldFallBackToLineWidthWhenOuterWallLineWidthIsNotNumeric()
        {
            var gcode = "; outer_wall_line_width = 105%\n; line_width = 0.5\n;Z:5\n" + Instance("Cube", 0, Box(10, 10, 20, 20));

            Assert.AreEqual("Models:\n  Cube  ×1   20.5 × 20.5 × 5.0 mm\n", Render(gcode));
        }

        [TestMethod]
        public void ShouldUsePrusaSlicerExtrusionWidthsWithTheExternalPerimeterFirst()
        {
            var body = ";Z:5\n" + Instance("Cube", 0, Box(10, 10, 20, 20));

            Assert.AreEqual("Models:\n  Cube  ×1   20.5 × 20.5 × 5.0 mm\n", Render("; external_perimeter_extrusion_width = 0.5\n; extrusion_width = 0.45\n" + body));
            // PrusaSlicer writes 0 for "auto" and a percentage of the layer height for a relative width; neither is usable.
            Assert.AreEqual("Models:\n  Cube  ×1   20.4 × 20.4 × 5.0 mm\n", Render("; external_perimeter_extrusion_width = 0\n; extrusion_width = 0.4\n" + body));
            Assert.AreEqual("Models:\n  Cube  ×1   20.4 × 20.4 × 5.0 mm\n", Render("; external_perimeter_extrusion_width = 120%\n; extrusion_width = 0.4\n" + body));
        }

        [TestMethod]
        public void ShouldUseBareCentrelineDimensionsWithoutAnyLineWidth()
        {
            var gcode = ";Z:5\n" + Instance("Cube", 0, Box(10, 10, 20, 20));

            Assert.AreEqual("Models:\n  Cube  ×1   20.0 × 20.0 × 5.0 mm\n", Render(gcode));
        }

        [TestMethod]
        public void ShouldRoundHalvesAwayFromZero()
        {
            var gcode = ";Z:5\n" + Instance("Cube", 0, Box(0, 0, 20.05, 20.04));

            Assert.AreEqual("Models:\n  Cube  ×1   20.1 × 20.0 × 5.0 mm\n", Render(gcode));
        }

        [TestMethod]
        public void ShouldRenderNothingWhenThereAreNoObjects()
        {
            Assert.AreEqual(string.Empty, Render(Header + ";Z:5\n;TYPE:Outer wall\nG1 X1 Y1 E1\n"));
        }

        [TestMethod]
        public void ShouldRenderNothingAndLogWhenTheGcodeCannotBeRead()
        {
            var log = new List<string>();

            var note = Render(Header, log.Add, () => throw new IOException("gone"));

            Assert.AreEqual(string.Empty, note);
            Assert.IsTrue(log.Any(l => l.Contains("gone")), string.Join("\n", log));
        }

        [TestMethod]
        public void ShouldDescribeTheBenchyFixtureAsOneGroupOfSixteen()
        {
            var gcode = TestData.Load(Path.Combine("OrcaSlicer", "orcaslicer-2.4.0-benchy-x16.gcode"));

            var note = Render(gcode);

            // The trimmed fixture keeps four layers; centreline extents plus the 0.45 mm outer wall.
            Assert.AreEqual("Models:\n  3DBenchy.drc  ×16   55.6 × 30.9 × 48.0 mm\n", note);
        }
    }
}
