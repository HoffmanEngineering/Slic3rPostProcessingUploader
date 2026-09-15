using System.Text;
using Slic3rPostProcessingUploader.Services.Parsers.Computed;

namespace Slic3rPostProcessingUploaderUnitTests.Services.Parsers.Computed
{
    /// <summary>
    /// The scanner streams a whole G-code file and computes, per object instance, the box its extrusions cover.
    /// Orca brackets each instance's moves with "; printing object …" / "; stop printing object …" on every layer.
    /// </summary>
    [TestClass]
    public class ObjectBoundsScannerTests
    {
        private static IReadOnlyList<ObjectBounds> Scan(string gcode, Action<string>? log = null)
        {
            using var stream = new MemoryStream(Encoding.UTF8.GetBytes(gcode));
            return ObjectBoundsScanner.Scan(stream, log ?? (_ => { }));
        }

        private static string Layer(string z, string body) => $";Z:{z}\n; printing object Cube id:0 copy 0\n{body}; stop printing object Cube id:0 copy 0\n";

        private const string Square = ";TYPE:Outer wall\nG1 X10 Y10 E0.5\nG1 X20 Y10 E0.5\nG1 X20 Y20 E0.5\nG1 X10 Y20 E0.5\n";

        private static void AssertBox(ObjectBounds b, double minX, double maxX, double minY, double maxY, double maxZ, double tolerance = 0.0001)
        {
            Assert.AreEqual(minX, b.MinX, tolerance, "MinX");
            Assert.AreEqual(maxX, b.MaxX, tolerance, "MaxX");
            Assert.AreEqual(minY, b.MinY, tolerance, "MinY");
            Assert.AreEqual(maxY, b.MaxY, tolerance, "MaxY");
            Assert.AreEqual(maxZ, b.MaxZ, tolerance, "MaxZ");
        }

        [TestMethod]
        public void ShouldBoxTheExtrusionsOfOneObject()
        {
            var result = Scan(Layer("0.2", Square));

            Assert.AreEqual(1, result.Count);
            Assert.AreEqual("Cube", result[0].Name);
            AssertBox(result[0], 10, 20, 10, 20, 0.2);
        }

        [TestMethod]
        public void ShouldAccumulateTheSameInstanceAcrossLayersIncludingAnEmptySpan()
        {
            var gcode = Layer("0.2", Square) + Layer("0.4", "") + Layer("0.6", ";TYPE:Outer wall\nG1 X25 Y5 E0.5\n") + Layer("0.8", "");

            var result = Scan(gcode);

            Assert.AreEqual(1, result.Count);
            AssertBox(result[0], 10, 25, 5, 20, 0.6);
        }

        [TestMethod]
        public void ShouldKeepCopiesOfAnObjectApart()
        {
            var gcode = ";Z:0.2\n" +
                "; printing object Cube id:0 copy 0\n" + Square + "; stop printing object Cube id:0 copy 0\n" +
                "; printing object Cube id:1 copy 0\n;TYPE:Outer wall\nG1 X50 Y50 E0.5\nG1 X60 Y60 E0.5\n; stop printing object Cube id:1 copy 0\n";

            var result = Scan(gcode);

            Assert.AreEqual(2, result.Count);
            AssertBox(result[0], 10, 20, 10, 20, 0.2);
            AssertBox(result[1], 50, 60, 50, 60, 0.2);
            Assert.AreEqual("Cube", result[1].Name);
        }

        [TestMethod]
        public void ShouldSwitchAccumulatorsWhenAStartFollowsAStartWithoutAStop()
        {
            var gcode = ";Z:0.2\n; printing object A id:0 copy 0\n" + Square + "; printing object B id:1 copy 0\n;TYPE:Outer wall\nG1 X50 Y50 E0.5\nG1 X60 Y60 E0.5\n";

            var result = Scan(gcode);

            Assert.AreEqual(2, result.Count);
            AssertBox(result[0], 10, 20, 10, 20, 0.2);
            AssertBox(result[1], 50, 60, 50, 60, 0.2);
        }

        [TestMethod]
        public void ShouldReadBambuStudioMarkersWhichCarryOnlyALabelId()
        {
            var gcode = "; Z_HEIGHT: 0.2\n; start printing object, unique label id: 67\n" + Square + "; stop printing object, unique label id: 67\n" +
                "; start printing object, unique label id: 100\n;TYPE:Outer wall\nG1 X50 Y50 E0.5\nG1 X60 Y60 E0.5\n; stop printing object, unique label id: 100\n" +
                "; Z_HEIGHT: 0.4\n; start printing object, unique label id: 67\n;TYPE:Outer wall\nG1 X10 Y10 E0.5\n; stop printing object, unique label id: 67\n";

            var result = Scan(gcode);

            Assert.AreEqual(2, result.Count);
            Assert.AreEqual("Object 67", result[0].Name);
            AssertBox(result[0], 10, 20, 10, 20, 0.4);
            Assert.AreEqual("Object 100", result[1].Name);
            AssertBox(result[1], 50, 60, 50, 60, 0.2);
        }

        [TestMethod]
        public void ShouldReadPrusaSlicerFirmwareLabelsWhereM486NamesTheInstanceOnce()
        {
            var gcode = "M486 S0\nM486 Acube20.stl\nM486 S1\nM486 Acube20.stl\nM486 S-1\n" +
                ";Z:0.2\nM486 S0\n" + Square + "M486 S-1\n" +
                "M486 S1\n;TYPE:Outer wall\nG1 X50 Y50 E0.5\nG1 X60 Y60 E0.5\nM486 S-1\n" +
                ";TYPE:Outer wall\nG1 X90 Y90 E0.5\n";

            var result = Scan(gcode);

            Assert.AreEqual(2, result.Count);
            Assert.AreEqual("cube20.stl", result[0].Name);
            AssertBox(result[0], 10, 20, 10, 20, 0.2);
            Assert.AreEqual("cube20.stl", result[1].Name);
            AssertBox(result[1], 50, 60, 50, 60, 0.2);
        }

        [TestMethod]
        public void ShouldNameAnUnlabelledM486InstanceByItsIndex()
        {
            var result = Scan(";Z:0.2\nM486 S3\n" + Square + "M486 S-1\n");

            Assert.AreEqual(1, result.Count);
            Assert.AreEqual("Object 3", result[0].Name);
        }

        [TestMethod]
        public void ShouldReadKlipperExcludeObjectMarkers()
        {
            var gcode = ";Z:0.2\nEXCLUDE_OBJECT_START NAME=cube20.stl_id_0_copy_0\n" + Square + "EXCLUDE_OBJECT_END NAME=cube20.stl_id_0_copy_0\n" +
                ";TYPE:Outer wall\nG1 X90 Y90 E0.5\n";

            var result = Scan(gcode);

            Assert.AreEqual(1, result.Count);
            Assert.AreEqual("cube20.stl", result[0].Name);
            AssertBox(result[0], 10, 20, 10, 20, 0.2);
        }

        [TestMethod]
        public void ShouldLetCommentMarkersWinWhenOrcaAlsoWritesM486ForTheSameInstance()
        {
            // Orca's Marlin flavour defines every instance with M486 up front, then brackets each layer with both the
            // comment and the M486 select; the comment carries the real name and must not lose its moves to M486.
            var gcode = "M486 S0\nM486 ACube_id_0_copy_0\nM486 S-1\n" +
                ";Z:0.2\n; printing object Cube id:0 copy 0\nM486 S0\n" + Square + "; stop printing object Cube id:0 copy 0\nM486 S-1\n";

            var result = Scan(gcode);

            Assert.AreEqual(1, result.Count);
            Assert.AreEqual("Cube", result[0].Name);
            AssertBox(result[0], 10, 20, 10, 20, 0.2);
        }

        [DataTestMethod]
        [DataRow("Brim")]
        [DataRow("Skirt")]
        [DataRow("Support")]
        [DataRow("Support interface")]
        [DataRow("Prime tower")]
        [DataRow("Wipe tower")]
        [DataRow("Custom")]
        public void ShouldIgnoreRunsThatAreNotPartOfTheModel(string type)
        {
            var body = $";TYPE:{type}\nG1 X0 Y0 E0.5\nG1 X100 Y100 E0.5\n" + Square;

            var result = Scan(Layer("0.2", body));

            AssertBox(result[0], 10, 20, 10, 20, 0.2);
        }

        [TestMethod]
        public void ShouldReadBambuStudioFeatureTagsLikeTypeTags()
        {
            var body = "; FEATURE: Prime tower\nG1 X0 Y0 E0.5\nG1 X100 Y100 E0.5\n; FEATURE: Outer wall\n" + Square[";TYPE:Outer wall\n".Length..];

            var result = Scan(Layer("0.2", body));

            AssertBox(result[0], 10, 20, 10, 20, 0.2);
        }

        [TestMethod]
        public void ShouldIgnoreMovesOutsideAnyObject()
        {
            var gcode = ";Z:0.2\n;TYPE:Outer wall\nG1 X0 Y0 E0.5\nG1 X100 Y100 E0.5\n" + Layer("0.2", Square) + "G1 X200 Y200 E0.5\n";

            var result = Scan(gcode);

            AssertBox(result[0], 10, 20, 10, 20, 0.2);
        }

        [TestMethod]
        public void ShouldUseModalCoordinatesForSingleAxisMoves()
        {
            var body = ";TYPE:Outer wall\nG1 X10 Y10 E0.5\nG1 X30 E0.5\nG1 Y40 E0.5\n";

            var result = Scan(Layer("0.2", body));

            AssertBox(result[0], 10, 30, 10, 40, 0.2);
        }

        [TestMethod]
        public void ShouldIgnoreTravelsRetractionsAndZeroExtrusion()
        {
            var body = Square + "G1 X90 Y90\nG1 X91 Y91 E-0.8\nG1 X92 Y92 E0\nG0 X93 Y93\n";

            var result = Scan(Layer("0.2", body));

            AssertBox(result[0], 10, 20, 10, 20, 0.2);
        }

        [TestMethod]
        public void ShouldDetectExtrusionInAbsoluteEMode()
        {
            var body = "M82\nG92 E0\n;TYPE:Outer wall\nG1 X10 Y10 E1\nG1 X20 Y20 E2\nG1 X90 Y90 E1.2\nG1 X91 Y91 E1.2\nG92 E0\nG1 X30 Y30 E0.5\nM83\nG1 X40 Y40 E0.1\nG1 X95 Y95 E-0.1\n";

            var result = Scan(Layer("0.2", body));

            // E1.2 after E2 is a retraction, E1.2 again is a travel, G92 resets, M83 returns to relative.
            AssertBox(result[0], 10, 40, 10, 40, 0.2);
        }

        [TestMethod]
        public void ShouldIncludeArcExtremaBetweenTheEndpoints()
        {
            // Quarter circle centred at (0,0), r=10, from 45° to 135° counter-clockwise: the top at y=10 is not an endpoint.
            var body = ";TYPE:Outer wall\nG1 X7.0711 Y7.0711 E0.1\nG3 X-7.0711 Y7.0711 I-7.0711 J-7.0711 E0.5\n";

            var result = Scan(Layer("0.2", body));

            AssertBox(result[0], -7.0711, 7.0711, 7.0711, 10, 0.2, tolerance: 0.001);
        }

        [TestMethod]
        public void ShouldIncludeClockwiseArcExtrema()
        {
            // Same points, clockwise: goes the long way round through 0°, -90°, 180°.
            var body = ";TYPE:Outer wall\nG1 X7.0711 Y7.0711 E0.1\nG2 X-7.0711 Y7.0711 I-7.0711 J-7.0711 E0.5\n";

            var result = Scan(Layer("0.2", body));

            AssertBox(result[0], -10, 10, -10, 7.0711, 0.2, tolerance: 0.001);
        }

        [TestMethod]
        public void ShouldNotIncludeArcExtremaOutsideTheSweep()
        {
            // 10° sweep near 45°: no cardinal point is crossed, box is just the endpoints.
            var body = ";TYPE:Outer wall\nG1 X7.0711 Y7.0711 E0.1\nG3 X6.4279 Y7.6604 I-7.0711 J-7.0711 E0.1\n";

            var result = Scan(Layer("0.2", body));

            AssertBox(result[0], 6.4279, 7.0711, 7.0711, 7.6604, 0.2, tolerance: 0.001);
        }

        [TestMethod]
        public void ShouldHandleAnArcCrossingZeroDegrees()
        {
            // From -45° to 45° counter-clockwise around (0,0): the rightmost point x=10 lies at 0°.
            var body = ";TYPE:Outer wall\nG1 X7.0711 Y-7.0711 E0.1\nG3 X7.0711 Y7.0711 I-7.0711 J7.0711 E0.5\n";

            var result = Scan(Layer("0.2", body));

            AssertBox(result[0], 7.0711, 10, -7.0711, 7.0711, 0.2, tolerance: 0.001);
        }

        [TestMethod]
        public void ShouldKeepObjectNamesWithSpacesAndLargeIds()
        {
            var gcode = ";Z:0.2\n; printing object Calibration Cube.stl id:4706452076321832960 copy 0\n" + Square;

            var result = Scan(gcode);

            Assert.AreEqual("Calibration Cube.stl", result[0].Name);
        }

        [TestMethod]
        public void ShouldIgnoreMalformedStartMarkers()
        {
            var gcode = ";Z:0.2\n; printing object nonsense\n" + Square;

            Assert.AreEqual(0, Scan(gcode).Count);
        }

        [TestMethod]
        public void ShouldReturnNothingWithoutObjectMarkers()
        {
            Assert.AreEqual(0, Scan(";Z:0.2\n" + Square).Count);
        }

        [TestMethod]
        public void ShouldDropInstancesThatNeverExtrude()
        {
            Assert.AreEqual(0, Scan(Layer("0.2", ";TYPE:Brim\nG1 X1 Y1 E0.5\n")).Count);
        }

        [TestMethod]
        public void ShouldAcceptCrlfBomAndAMissingFinalNewline()
        {
            var gcode = "﻿" + (";Z:0.2\n; printing object Cube id:0 copy 0\n" + Square).Replace("\n", "\r\n").TrimEnd('\r', '\n');

            var result = Scan(gcode);

            AssertBox(result[0], 10, 20, 10, 20, 0.2);
        }

        [TestMethod]
        public void ShouldDecodeNonAsciiNamesSplitAcrossBufferBoundaries()
        {
            // Pad so the object marker straddles the 1 MB read boundary, then check the name survives intact.
            var filler = new StringBuilder();
            while (filler.Length < ObjectBoundsScanner.BufferSize - 20)
            {
                filler.Append("; padding line\n");
            }

            var gcode = filler + ";Z:0.2\n; printing object Würfel Ünïcode id:0 copy 0\n" + Square;

            var result = Scan(gcode);

            Assert.AreEqual(1, result.Count);
            Assert.AreEqual("Würfel Ünïcode", result[0].Name);
            AssertBox(result[0], 10, 20, 10, 20, 0.2);
        }

        [TestMethod]
        public void ShouldCopeWithStreamsThatReturnShortReads()
        {
            var bytes = Encoding.UTF8.GetBytes(Layer("0.2", Square));
            using var stream = new OneByteAtATimeStream(bytes);

            var result = ObjectBoundsScanner.Scan(stream, _ => { });

            AssertBox(result[0], 10, 20, 10, 20, 0.2);
        }

        [TestMethod]
        public void ShouldSkipALineLongerThanTheBufferAndCarryOn()
        {
            var log = new List<string>();
            var gcode = ";Z:0.2\n; " + new string('x', ObjectBoundsScanner.BufferSize + 10) + "\n" + "; printing object Cube id:0 copy 0\n" + Square;

            var result = Scan(gcode, log.Add);

            AssertBox(result[0], 10, 20, 10, 20, 0.2);
            Assert.IsTrue(log.Count > 0);
        }

        [TestMethod]
        public void ShouldPropagateStreamFailures()
        {
            using var stream = new ThrowingStream();

            Assert.ThrowsException<IOException>(() => ObjectBoundsScanner.Scan(stream, _ => { }));
        }

        [TestMethod]
        public void ShouldBoxEveryBenchyInTheRealFixture()
        {
            // The fixture keeps four layers of the real export, so the union is narrower than a whole Benchy (60 x 31).
            using var stream = File.OpenRead(TestData.FullPath(Path.Combine("OrcaSlicer", "orcaslicer-2.4.0-benchy-x16.gcode")));

            var result = ObjectBoundsScanner.Scan(stream, _ => { });

            Assert.AreEqual(16, result.Count);
            foreach (var box in result)
            {
                Assert.AreEqual("3DBenchy.drc", box.Name);
                Assert.AreEqual(55.2, box.MaxX - box.MinX, 0.05, "width");
                Assert.AreEqual(30.5, box.MaxY - box.MinY, 0.05, "depth");
                Assert.AreEqual(48.0, box.MaxZ, 0.01, "height");
            }
        }

        private sealed class OneByteAtATimeStream(byte[] bytes) : MemoryStream(bytes)
        {
            public override int Read(byte[] buffer, int offset, int count) => base.Read(buffer, offset, Math.Min(count, 1));
            public override int Read(Span<byte> buffer) => base.Read(buffer[..Math.Min(buffer.Length, 1)]);
        }

        private sealed class ThrowingStream : MemoryStream
        {
            public override int Read(byte[] buffer, int offset, int count) => throw new IOException("disk on fire");
            public override int Read(Span<byte> buffer) => throw new IOException("disk on fire");
        }

        // ---- Cases from the adversarial review ----

        [TestMethod]
        public void ShouldNotAddAPointForAnExtrusionOnlyMove()
        {
            // Orca de-retracts (G1 E.7) at the travel destination before announcing the next ;TYPE:; a move with no
            // X/Y has no path and must not widen the box, whatever the current role is.
            var gcode = ";Z:0.2\n; printing object A id:0 copy 0\n" + Square + "; stop printing object A id:0 copy 0\n" +
                "; printing object B id:1 copy 0\nG1 X150 Y150 F18000\nG1 E.7 F1500\n;TYPE:Brim\nG1 X151 Y151 E0.5\n;TYPE:Outer wall\nG1 X50 Y50 E0.5\nG1 X60 Y60 E0.5\n";

            var result = Scan(gcode);

            AssertBox(result[1], 50, 60, 50, 60, 0.2);
        }

        [TestMethod]
        public void ShouldTreatACoincidentEndpointArcAsAFullCircle()
        {
            var body = ";TYPE:Outer wall\nG1 X10 Y0 E0.1\nG3 X10 Y0 I-10 J0 E1\n";

            var result = Scan(Layer("0.2", body));

            AssertBox(result[0], -10, 10, -10, 10, 0.2, tolerance: 0.001);
        }

        [TestMethod]
        public void ShouldKeepTheEPositionAcrossARelativeToAbsoluteSwitch()
        {
            // G92 E0, extrude 5 relatively, then in absolute mode E4 is a retraction, not extrusion.
            var body = "G92 E0\nM83\n;TYPE:Outer wall\nG1 X10 Y10 E5\nM82\nG1 X90 Y90 E4\nG1 X20 Y20 E6\n";

            var result = Scan(Layer("0.2", body));

            AssertBox(result[0], 10, 20, 10, 20, 0.2);
        }

        [TestMethod]
        public void ShouldStripTheBomEvenWhenTheFirstReadIsShort()
        {
            var bytes = Encoding.UTF8.GetBytes("﻿" + Layer("0.2", Square));
            using var stream = new OneByteAtATimeStream(bytes);

            var result = ObjectBoundsScanner.Scan(stream, _ => { });

            AssertBox(result[0], 10, 20, 10, 20, 0.2);
        }

        [DataTestMethod]
        [DataRow(";Z:12junk")]
        [DataRow(";Z:NaN")]
        [DataRow(";Z:")]
        public void ShouldIgnoreMalformedLayerHeights(string layerLine)
        {
            var gcode = ";Z:0.2\n" + layerLine + "\n; printing object Cube id:0 copy 0\n" + Square;

            var result = Scan(gcode);

            AssertBox(result[0], 10, 20, 10, 20, 0.2);
        }
    }
}
