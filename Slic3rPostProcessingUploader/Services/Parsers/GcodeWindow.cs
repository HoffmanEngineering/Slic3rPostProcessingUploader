using System.Text;

namespace Slic3rPostProcessingUploader.Services.Parsers
{
    /// <summary>
    /// Every supported slicer writes its metadata (header, thumbnails, settings block) at the very start and/or very end
    /// of the file, with the bulk of the file being toolpath commands in between. For large files, only the head and tail
    /// need to be searched, which keeps parse time independent of file size.
    /// </summary>
    internal static class GcodeWindow
    {
        /// <summary>
        /// Size of the head and of the tail that are kept, in chars (for strings) or bytes (for files). Must comfortably
        /// exceed the largest thumbnail block a slicer emits (a 720p PNG is a few hundred KB of base64).
        /// </summary>
        public const int WindowSize = 2_000_000;

        /// <summary>
        /// Returns the gcode unchanged when it fits within two windows, otherwise the head and tail joined by a newline.
        /// Head and tail are kept in their original order so "first match wins" lookups behave the same as on the full file.
        /// </summary>
        public static string Trim(string gcode)
        {
            if (gcode.Length <= WindowSize * 2)
            {
                return gcode;
            }

            return string.Concat(gcode.AsSpan(0, WindowSize), "\n", gcode.AsSpan(gcode.Length - WindowSize));
        }

        /// <summary>
        /// Reads only the head and tail of a large file from disk. Small files are read in full.
        /// </summary>
        public static string ReadFromFile(string path)
        {
            using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);

            if (stream.Length <= WindowSize * 2L)
            {
                using var reader = new StreamReader(stream, Encoding.UTF8, detectEncodingFromByteOrderMarks: true);
                return reader.ReadToEnd();
            }

            var head = new byte[WindowSize];
            stream.ReadExactly(head);

            var tail = new byte[WindowSize];
            stream.Seek(-WindowSize, SeekOrigin.End);
            stream.ReadExactly(tail);

            // A BOM would otherwise survive as U+FEFF at the start of the string. The tail may begin mid-character;
            // UTF8 decoding turns that into a replacement char inside a line we are not interested in anyway.
            return string.Concat(Encoding.UTF8.GetString(head).TrimStart('﻿'), "\n", Encoding.UTF8.GetString(tail));
        }
    }
}
