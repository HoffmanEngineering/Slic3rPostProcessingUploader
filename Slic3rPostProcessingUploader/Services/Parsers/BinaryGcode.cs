using System.IO.Compression;
using System.Text;

namespace Slic3rPostProcessingUploader.Services.Parsers
{
    /// <summary>
    /// Reads PrusaSlicer binary G-code (libbgcode v1, magic "GCDE") and rewrites its metadata blocks as the ASCII
    /// header a PrusaSlicer text export would have, so the rest of the pipeline is unaware of the binary format.
    /// The G-code blocks themselves (Heatshrink + MeatPack) are never decoded: only the metadata is needed here.
    /// </summary>
    internal static class BinaryGcode
    {
        private const uint SupportedVersion = 1;
        private const int Base64LineLength = 78;

        private static readonly byte[] Magic = "GCDE"u8.ToArray();

        private enum BlockType : ushort
        {
            FileMetadata = 0,
            GCode = 1,
            SlicerMetadata = 2,
            PrinterMetadata = 3,
            PrintMetadata = 4,
            Thumbnail = 5,
        }

        private enum Compression : ushort
        {
            None = 0,
            Deflate = 1,
            Heatshrink11 = 2,
            Heatshrink12 = 3,
        }

        private enum ThumbnailFormat : ushort
        {
            Png = 0,
            Jpg = 1,
            Qoi = 2,
        }

        private const string CorruptMessage = "The binary G-code file appears to be corrupt or truncated.";
        private const string ReExportHint = "Re-export the G-code from the slicer.";
        private const string AsciiHint = "Re-export as ASCII G-code (uncheck 'Supports binary G-code' in Printer Settings) or update this tool.";

        public static bool IsBinaryGcode(ReadOnlySpan<byte> head)
        {
            return head.Length >= Magic.Length && head[..Magic.Length].SequenceEqual(Magic);
        }

        public static bool IsBinaryGcodeFile(string path)
        {
            using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);
            var head = new byte[Magic.Length];
            int read = stream.Read(head, 0, head.Length);
            return IsBinaryGcode(head.AsSpan(0, read));
        }

        public static string ReadFromFile(string path)
        {
            using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);
            return Decode(stream);
        }

        /// <summary>
        /// Walks every block in the stream and returns the synthetic ASCII G-code header.
        /// </summary>
        public static string Decode(Stream stream)
        {
            var reader = new BinaryReader(stream);
            var output = new StringBuilder();

            try
            {
                var magic = reader.ReadBytes(Magic.Length);
                if (!IsBinaryGcode(magic))
                {
                    throw new UserFacingException("The file is not a binary G-code file.", ReExportHint);
                }

                uint version = reader.ReadUInt32();
                if (version != SupportedVersion)
                {
                    throw new UserFacingException($"Unsupported binary G-code version {version}.", AsciiHint);
                }

                bool hasChecksum = reader.ReadUInt16() != 0;

                var slicerConfig = new StringBuilder();

                while (stream.Position < stream.Length)
                {
                    ReadBlock(reader, hasChecksum, output, slicerConfig);
                }

                if (slicerConfig.Length > 0)
                {
                    output.Append(";\n; prusaslicer_config = begin\n");
                    output.Append(slicerConfig);
                    output.Append("; prusaslicer_config = end\n");
                }
            }
            catch (EndOfStreamException ex)
            {
                throw new UserFacingException(CorruptMessage, ReExportHint, ex);
            }
            catch (InvalidDataException ex)
            {
                throw new UserFacingException(CorruptMessage, ReExportHint, ex);
            }

            return output.ToString();
        }

        private static void ReadBlock(BinaryReader reader, bool hasChecksum, StringBuilder output, StringBuilder slicerConfig)
        {
            long blockStart = reader.BaseStream.Position;

            var type = (BlockType)reader.ReadUInt16();
            var compression = (Compression)reader.ReadUInt16();
            uint uncompressedSize = reader.ReadUInt32();
            uint dataSize = compression == Compression.None ? uncompressedSize : reader.ReadUInt32();
            int parameterSize = type == BlockType.Thumbnail ? 6 : 2;

            // The G-code body is the bulk of the file and irrelevant here, so it is skipped rather than read.
            if (type == BlockType.GCode)
            {
                Skip(reader, parameterSize + dataSize + (hasChecksum ? 4 : 0));
                return;
            }

            var parameters = reader.ReadBytes(parameterSize);
            var data = ReadExactly(reader, dataSize);

            if (hasChecksum)
            {
                uint expected = reader.ReadUInt32();
                long blockEnd = reader.BaseStream.Position - 4;
                if (Crc32(reader.BaseStream, blockStart, blockEnd) != expected)
                {
                    throw new UserFacingException(CorruptMessage, ReExportHint);
                }
            }

            var payload = Decompress(type, compression, data, uncompressedSize);

            switch (type)
            {
                case BlockType.FileMetadata:
                    AppendProducerLine(output, payload);
                    break;
                case BlockType.PrinterMetadata:
                case BlockType.PrintMetadata:
                    output.Append(";\n");
                    AppendSettings(output, payload);
                    break;
                case BlockType.SlicerMetadata:
                    AppendSettings(slicerConfig, payload);
                    break;
                case BlockType.Thumbnail:
                    AppendThumbnail(output, parameters, payload);
                    break;
                default:
                    // Unknown block types are skipped so newer files still yield their metadata.
                    break;
            }
        }

        private static byte[] Decompress(BlockType type, Compression compression, byte[] data, uint uncompressedSize)
        {
            switch (compression)
            {
                case Compression.None:
                    return data;

                case Compression.Deflate:
                    using (var input = new MemoryStream(data))
                    using (var zlib = new ZLibStream(input, CompressionMode.Decompress))
                    using (var result = new MemoryStream((int)uncompressedSize))
                    {
                        zlib.CopyTo(result);
                        return result.ToArray();
                    }

                default:
                    // PrusaSlicer only uses Heatshrink for the G-code body, which is never read here.
                    throw new UserFacingException(
                        $"Binary G-code block '{Describe(type)}' uses Heatshrink compression, which this tool does not support.",
                        AsciiHint);
            }
        }

        private static string Describe(BlockType type) => type switch
        {
            BlockType.FileMetadata => "file metadata",
            BlockType.SlicerMetadata => "slicer metadata",
            BlockType.PrinterMetadata => "printer metadata",
            BlockType.PrintMetadata => "print metadata",
            BlockType.Thumbnail => "thumbnail",
            _ => type.ToString(),
        };

        /// <summary>
        /// "Producer=PrusaSlicer 2.9.2" + "Produced on=..." become the "; generated by PrusaSlicer 2.9.2 on ..." line
        /// that the parsers use for detection and version extraction.
        /// </summary>
        private static void AppendProducerLine(StringBuilder output, byte[] payload)
        {
            string? producer = null;
            string? producedOn = null;

            foreach (var (key, value) in ReadIni(payload))
            {
                if (key.Equals("Producer", StringComparison.OrdinalIgnoreCase))
                {
                    producer = value;
                }
                else if (key.Equals("Produced on", StringComparison.OrdinalIgnoreCase))
                {
                    producedOn = value;
                }
            }

            if (producer != null)
            {
                output.Append("; generated by ").Append(producer).Append(" on ").Append(producedOn ?? "unknown").Append('\n');
            }
        }

        private static void AppendSettings(StringBuilder output, byte[] payload)
        {
            foreach (var (key, value) in ReadIni(payload))
            {
                output.Append("; ").Append(key);
                if (value != null)
                {
                    output.Append(" = ").Append(value);
                }
                output.Append('\n');
            }
        }

        /// <summary>
        /// Metadata blocks are INI-style "key=value" lines. Only the first '=' separates key from value, since values
        /// such as objects_info contain '=' themselves.
        /// </summary>
        private static IEnumerable<(string key, string? value)> ReadIni(byte[] payload)
        {
            var text = Encoding.UTF8.GetString(payload);
            foreach (var rawLine in text.Split('\n'))
            {
                var line = rawLine.TrimEnd('\r');
                if (line.Length == 0)
                {
                    continue;
                }

                int separator = line.IndexOf('=');
                if (separator < 0)
                {
                    yield return (line, null);
                }
                else
                {
                    yield return (line[..separator].Trim(), line[(separator + 1)..].Trim());
                }
            }
        }

        /// <summary>
        /// PNG and JPG thumbnails are written the way slicers embed them in ASCII G-code so GetSnapshot picks them
        /// up unchanged. QOI is skipped because browsers cannot render it.
        /// </summary>
        private static void AppendThumbnail(StringBuilder output, byte[] parameters, byte[] payload)
        {
            var format = (ThumbnailFormat)BitConverter.ToUInt16(parameters, 0);
            ushort width = BitConverter.ToUInt16(parameters, 2);
            ushort height = BitConverter.ToUInt16(parameters, 4);

            if (format == ThumbnailFormat.Qoi)
            {
                return;
            }

            string label = format == ThumbnailFormat.Jpg ? "thumbnail_JPG" : "thumbnail";
            var base64 = Convert.ToBase64String(payload);

            output.Append(";\n; ").Append(label).Append(" begin ").Append(width).Append('x').Append(height).Append(' ').Append(payload.Length).Append('\n');
            for (int i = 0; i < base64.Length; i += Base64LineLength)
            {
                output.Append("; ").Append(base64, i, Math.Min(Base64LineLength, base64.Length - i)).Append('\n');
            }
            output.Append("; ").Append(label).Append(" end\n");
        }

        private static void Skip(BinaryReader reader, long count)
        {
            if (reader.BaseStream.Position + count > reader.BaseStream.Length)
            {
                throw new EndOfStreamException();
            }

            reader.BaseStream.Seek(count, SeekOrigin.Current);
        }

        private static byte[] ReadExactly(BinaryReader reader, uint count)
        {
            var bytes = reader.ReadBytes(checked((int)count));
            if (bytes.Length != count)
            {
                throw new EndOfStreamException();
            }

            return bytes;
        }

        private static uint Crc32(Stream stream, long start, long end)
        {
            long resume = stream.Position;
            stream.Position = start;
            var bytes = new byte[end - start];
            stream.ReadExactly(bytes);
            stream.Position = resume;
            return Crc32(bytes);
        }

        private static readonly uint[] CrcTable = BuildCrcTable();

        private static uint[] BuildCrcTable()
        {
            var table = new uint[256];
            for (uint i = 0; i < table.Length; i++)
            {
                uint c = i;
                for (int k = 0; k < 8; k++)
                {
                    c = (c & 1) != 0 ? 0xEDB88320u ^ (c >> 1) : c >> 1;
                }
                table[i] = c;
            }
            return table;
        }

        /// <summary>
        /// Standard IEEE CRC-32, as used for libbgcode block checksums (covering header, parameters and data).
        /// </summary>
        public static uint Crc32(ReadOnlySpan<byte> bytes)
        {
            uint crc = 0xFFFFFFFFu;
            foreach (var b in bytes)
            {
                crc = CrcTable[(crc ^ b) & 0xFF] ^ (crc >> 8);
            }
            return ~crc;
        }
    }
}
