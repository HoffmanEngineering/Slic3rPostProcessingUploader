using System.Buffers.Text;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace Slic3rPostProcessingUploader.Services.Parsers.Computed
{
    /// <summary>
    /// The box one object instance's extrusions cover, in bed coordinates. Z is the top of the last layer that extruded.
    /// </summary>
    internal sealed class ObjectBounds(string name)
    {
        public string Name { get; internal set; } = name;
        public double MinX { get; private set; } = double.PositiveInfinity;
        public double MaxX { get; private set; } = double.NegativeInfinity;
        public double MinY { get; private set; } = double.PositiveInfinity;
        public double MaxY { get; private set; } = double.NegativeInfinity;
        public double MaxZ { get; private set; }
        public bool HasPoints { get; private set; }

        internal void Add(double x, double y, double z)
        {
            if (x < MinX) MinX = x;
            if (x > MaxX) MaxX = x;
            if (y < MinY) MinY = y;
            if (y > MaxY) MaxY = y;
            if (z > MaxZ) MaxZ = z;
            HasPoints = true;
        }
    }

    /// <summary>
    /// Streams a whole G-code file and computes a bounding box per object instance. Every supported slicer tags
    /// extrusion runs with their role (";TYPE:", or "; FEATURE:" in Bambu Studio, with the same role names) and
    /// brackets each instance's toolpath on every layer, each in its own dialect:
    /// <list type="bullet">
    /// <item>Orca, and PrusaSlicer in octoprint mode: "; printing object &lt;name&gt; id:&lt;n&gt; copy &lt;m&gt;" …
    /// "; stop printing object …", layers announced with ";Z:"</item>
    /// <item>Bambu Studio: "; start printing object, unique label id: &lt;n&gt;" … "; stop printing object, unique label
    /// id: &lt;n&gt;", layers announced with "; Z_HEIGHT:". The file never names the objects, so an instance is "Object
    /// &lt;n&gt;"</item>
    /// <item>PrusaSlicer firmware mode: "M486 S&lt;n&gt;" starts instance n (named once, up front, by "M486 A&lt;name&gt;"),
    /// "M486 S-1" stops; Klipper flavour uses "EXCLUDE_OBJECT_START NAME=…" / "EXCLUDE_OBJECT_END"</item>
    /// </list>
    /// Orca writes the comment markers and the firmware commands for the same instance in one file, so once a comment
    /// marker has been seen the firmware commands are ignored: the comments carry the real names.
    /// Only extruding moves count, and brim, skirt, supports and towers are left out so the box is the model itself.
    ///
    /// The scan works on bytes to stay cheap on 100+ MB files: numbers and keywords are parsed in place and only
    /// object names are decoded. The file is assumed to be UTF-8 (Orca's output); invalid sequences in a name become
    /// replacement characters.
    /// </summary>
    internal static partial class ObjectBoundsScanner
    {
        public const int BufferSize = 1 << 20;

        /// <summary>
        /// Firmware object names are the comment marker flattened for the firmware's character set:
        /// "3DBenchy.drc_id_0_copy_0". The suffix identifies the instance, not the model.
        /// </summary>
        [GeneratedRegex(@"_id_\d+_copy_\d+$")]
        private static partial Regex FirmwareInstanceSuffix();

        private static ReadOnlySpan<byte> StartMarker => "; printing object "u8;
        private static ReadOnlySpan<byte> StopMarker => "; stop printing object"u8;
        private static ReadOnlySpan<byte> BambuStartMarker => "; start printing object, unique label id:"u8;
        private static ReadOnlySpan<byte> KlipperStartMarker => "EXCLUDE_OBJECT_START NAME="u8;
        private static ReadOnlySpan<byte> KlipperStopMarker => "EXCLUDE_OBJECT_END"u8;
        private static ReadOnlySpan<byte> TypeMarker => ";TYPE:"u8;
        private static ReadOnlySpan<byte> BambuTypeMarker => "; FEATURE:"u8;
        private static ReadOnlySpan<byte> LayerMarker => ";Z:"u8;
        private static ReadOnlySpan<byte> BambuLayerMarker => "; Z_HEIGHT:"u8;
        private static ReadOnlySpan<byte> Bom => [0xEF, 0xBB, 0xBF];

        // Extrusion roles that are not part of the model. Compared as prefixes ("Support interface" etc.).
        private static readonly byte[][] SkippedTypes =
        [
            "Brim"u8.ToArray(), "Skirt"u8.ToArray(), "Support"u8.ToArray(), "Prime tower"u8.ToArray(), "Wipe tower"u8.ToArray(), "Custom"u8.ToArray(),
        ];

        public static IReadOnlyList<ObjectBounds> Scan(Stream stream, Action<string> debugLog)
        {
            var state = new ScanState(debugLog);
            var buffer = new byte[BufferSize];
            int length = 0;
            bool first = true;
            bool skippingLongLine = false;

            while (true)
            {
                int read = stream.Read(buffer, length, buffer.Length - length);
                length += read;

                int start = 0;
                if (first)
                {
                    // Wait until enough bytes have arrived to decide (a short first read must not skip the check).
                    if (length < Bom.Length && read > 0)
                    {
                        continue;
                    }

                    first = false;
                    if (length >= Bom.Length && buffer.AsSpan(0, Bom.Length).SequenceEqual(Bom))
                    {
                        start = Bom.Length;
                    }
                }

                while (true)
                {
                    int newline = Array.IndexOf(buffer, (byte)'\n', start, length - start);
                    if (newline < 0)
                    {
                        break;
                    }

                    if (skippingLongLine)
                    {
                        skippingLongLine = false;
                    }
                    else
                    {
                        state.ProcessLine(TrimCr(buffer.AsSpan(start, newline - start)));
                    }

                    start = newline + 1;
                }

                if (read == 0)
                {
                    // End of stream: whatever is left is the final line without a terminator.
                    if (!skippingLongLine && length > start)
                    {
                        state.ProcessLine(TrimCr(buffer.AsSpan(start, length - start)));
                    }

                    break;
                }

                if (start == 0 && length == buffer.Length)
                {
                    // A single line longer than the buffer. Nothing we care about is that long; drop it and resync at
                    // the next newline.
                    debugLog($"Skipping a G-code line longer than {BufferSize} bytes");
                    skippingLongLine = true;
                    length = 0;
                    continue;
                }

                // Carry the partial last line to the front of the buffer.
                Buffer.BlockCopy(buffer, start, buffer, 0, length - start);
                length -= start;
            }

            return state.Results();
        }

        private static ReadOnlySpan<byte> TrimCr(ReadOnlySpan<byte> line) =>
            line.Length > 0 && line[^1] == '\r' ? line[..^1] : line;

        private sealed class ScanState(Action<string> debugLog)
        {
            private readonly Dictionary<string, ObjectBounds> instances = new(StringComparer.Ordinal);
            private readonly List<ObjectBounds> order = [];
            private ObjectBounds? active;
            private bool commentMarkersSeen;
            private bool skipType;
            private double x, y, z;
            private bool relativeE = true;
            private double lastE;

            public IReadOnlyList<ObjectBounds> Results() => order.Where(o => o.HasPoints).ToList();

            public void ProcessLine(ReadOnlySpan<byte> line)
            {
                if (line.Length == 0)
                {
                    return;
                }

                if (line[0] == 'G')
                {
                    ProcessMove(line);
                }
                else if (line[0] == ';')
                {
                    ProcessComment(line);
                }
                else if (line[0] == 'M')
                {
                    if (IsCommand(line, "M82"u8)) relativeE = false;
                    else if (IsCommand(line, "M83"u8)) relativeE = true;
                    else if (IsCommand(line, "M486"u8) && !commentMarkersSeen) ProcessM486(line["M486"u8.Length..].TrimStart((byte)' '));
                }
                else if (commentMarkersSeen)
                {
                    return;
                }
                else if (line.StartsWith(KlipperStartMarker))
                {
                    var name = Encoding.UTF8.GetString(line[KlipperStartMarker.Length..].Trim((byte)' '));
                    Activate("klipper:" + name, FirmwareInstanceSuffix().Replace(name, string.Empty));
                }
                else if (line.StartsWith(KlipperStopMarker))
                {
                    active = null;
                }
            }

            /// <summary>
            /// "M486 S&lt;n&gt;" selects instance n (S-1 deselects); "M486 A&lt;name&gt;" names the selected instance, which
            /// PrusaSlicer does once for every instance before the first layer.
            /// </summary>
            private void ProcessM486(ReadOnlySpan<byte> arguments)
            {
                if (arguments.Length < 2)
                {
                    return;
                }

                if (arguments[0] == 'S')
                {
                    var value = arguments[1..];
                    int end = value.IndexOf((byte)' ');
                    if (end >= 0)
                    {
                        value = value[..end];
                    }

                    if (Utf8Parser.TryParse(value, out int index, out int consumed) && consumed == value.Length && index >= 0)
                    {
                        Activate("m486:" + index.ToString(CultureInfo.InvariantCulture), "Object " + index.ToString(CultureInfo.InvariantCulture));
                    }
                    else
                    {
                        active = null;
                    }
                }
                else if (arguments[0] == 'A' && active != null)
                {
                    active.Name = FirmwareInstanceSuffix().Replace(Encoding.UTF8.GetString(arguments[1..].Trim((byte)' ')), string.Empty);
                }
            }

            private void ProcessComment(ReadOnlySpan<byte> line)
            {
                if (line.StartsWith(StartMarker))
                {
                    commentMarkersSeen = true;
                    StartInstance(line[StartMarker.Length..]);
                }
                else if (line.StartsWith(BambuStartMarker))
                {
                    commentMarkersSeen = true;
                    var id = Encoding.UTF8.GetString(line[BambuStartMarker.Length..].Trim((byte)' '));
                    Activate("bambu:" + id, "Object " + id);
                }
                else if (line.StartsWith(StopMarker))
                {
                    active = null;
                }
                else if (line.StartsWith(BambuLayerMarker))
                {
                    SetLayer(line[BambuLayerMarker.Length..]);
                }
                else if (line.StartsWith(TypeMarker))
                {
                    SetType(line[TypeMarker.Length..]);
                }
                else if (line.StartsWith(BambuTypeMarker))
                {
                    SetType(line[BambuTypeMarker.Length..].TrimStart((byte)' '));
                }
                else if (line.StartsWith(LayerMarker))
                {
                    SetLayer(line[LayerMarker.Length..]);
                }
            }

            private void SetType(ReadOnlySpan<byte> type)
            {
                skipType = false;
                foreach (var skipped in SkippedTypes)
                {
                    if (type.StartsWith(skipped))
                    {
                        skipType = true;
                        break;
                    }
                }
            }

            private void SetLayer(ReadOnlySpan<byte> value)
            {
                value = value.Trim((byte)' ');
                if (Utf8Parser.TryParse(value, out double layerZ, out int consumed) && consumed == value.Length && double.IsFinite(layerZ))
                {
                    z = layerZ;
                }
            }

            /// <summary>
            /// Makes the instance identified by <paramref name="key"/> the one that receives moves, creating it under
            /// <paramref name="name"/> on first sight. Keys are prefixed per dialect so an id never collides across them.
            /// </summary>
            private void Activate(string key, string name)
            {
                if (!instances.TryGetValue(key, out active))
                {
                    active = new ObjectBounds(name);
                    instances[key] = active;
                    order.Add(active);
                }
            }

            /// <summary>
            /// The marker text after "; printing object " is "&lt;name&gt; id:&lt;n&gt; copy &lt;m&gt;". The whole text identifies the
            /// instance (a copy has its own id; the name may contain spaces), so it is parsed from the end.
            /// </summary>
            private void StartInstance(ReadOnlySpan<byte> marker)
            {
                int copyAt = marker.LastIndexOf(" copy "u8);
                int idAt = copyAt < 0 ? -1 : marker[..copyAt].LastIndexOf(" id:"u8);
                if (idAt <= 0)
                {
                    debugLog($"Ignoring object marker without id/copy: {Encoding.UTF8.GetString(marker)}");
                    active = null;
                    return;
                }

                Activate("orca:" + Encoding.UTF8.GetString(marker), Encoding.UTF8.GetString(marker[..idAt]));
            }

            private void ProcessMove(ReadOnlySpan<byte> line)
            {
                bool isLinear = IsCommand(line, "G0"u8) || IsCommand(line, "G1"u8);
                bool clockwise = IsCommand(line, "G2"u8);
                bool isArc = clockwise || IsCommand(line, "G3"u8);
                if (!isLinear && !isArc)
                {
                    if (IsCommand(line, "G92"u8) && TryGetWord(line, (byte)'E', out var reset))
                    {
                        lastE = reset;
                    }

                    return;
                }

                double startX = x, startY = y;
                bool moves = false;
                if (TryGetWord(line, (byte)'X', out var nx)) { x = nx; moves = true; }
                if (TryGetWord(line, (byte)'Y', out var ny)) { y = ny; moves = true; }

                // The filament position is tracked in both modes so a later M82 compares against the real value.
                bool extrudes = false;
                if (TryGetWord(line, (byte)'E', out var e))
                {
                    double delta = relativeE ? e : e - lastE;
                    lastE += delta;
                    extrudes = delta > 0;
                }

                // A de-retraction (E without X/Y) has no path, so it never widens the box.
                if (!moves || !extrudes || active == null || skipType)
                {
                    return;
                }

                active.Add(x, y, z);
                if (isArc && TryGetWord(line, (byte)'I', out var i) && TryGetWord(line, (byte)'J', out var j))
                {
                    AddArcExtrema(active, startX, startY, x, y, startX + i, startY + j, clockwise);
                }
            }

            /// <summary>
            /// An arc reaches its extreme X or Y wherever it crosses 0°, 90°, 180° or 270° around its centre, which
            /// need not be at either endpoint. Each cardinal point that lies within the sweep is folded into the box.
            /// </summary>
            private void AddArcExtrema(ObjectBounds bounds, double x0, double y0, double x1, double y1, double cx, double cy, bool clockwise)
            {
                double radius = Math.Sqrt((x0 - cx) * (x0 - cx) + (y0 - cy) * (y0 - cy));
                double startAngle = Math.Atan2(y0 - cy, x0 - cx);
                double endAngle = Math.Atan2(y1 - cy, x1 - cx);
                double sweep = clockwise ? Normalise(startAngle - endAngle) : Normalise(endAngle - startAngle);
                if (sweep < 1e-9 && radius > 0)
                {
                    // Coincident endpoints on a real circle mean a full turn, not no turn.
                    sweep = 2 * Math.PI;
                }

                for (int quadrant = 0; quadrant < 4; quadrant++)
                {
                    double angle = quadrant * Math.PI / 2;
                    double offset = clockwise ? Normalise(startAngle - angle) : Normalise(angle - startAngle);
                    if (offset <= sweep)
                    {
                        bounds.Add(cx + radius * Math.Cos(angle), cy + radius * Math.Sin(angle), z);
                    }
                }
            }

            private static double Normalise(double angle)
            {
                const double tau = 2 * Math.PI;
                angle %= tau;
                return angle < 0 ? angle + tau : angle;
            }

            private static bool IsCommand(ReadOnlySpan<byte> line, ReadOnlySpan<byte> command) =>
                line.StartsWith(command) && (line.Length == command.Length || line[command.Length] == ' ');

            /// <summary>
            /// Finds "&lt;letter&gt;&lt;number&gt;" as a whole word, stopping at a ';' comment.
            /// </summary>
            private static bool TryGetWord(ReadOnlySpan<byte> line, byte letter, out double value)
            {
                foreach (var range in line.Split((byte)' '))
                {
                    var token = line[range];
                    if (token.Length == 0)
                    {
                        continue;
                    }

                    if (token[0] == ';')
                    {
                        break;
                    }

                    if (token[0] == letter && token.Length > 1)
                    {
                        return Utf8Parser.TryParse(token[1..], out value, out int consumed) && consumed == token.Length - 1;
                    }
                }

                value = 0;
                return false;
            }
        }
    }
}
