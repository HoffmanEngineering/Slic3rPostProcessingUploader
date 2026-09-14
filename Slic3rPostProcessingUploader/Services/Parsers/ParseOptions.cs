using System.Text;

namespace Slic3rPostProcessingUploader.Services.Parsers
{
    /// <summary>
    /// Per-run inputs a parser needs beyond the (windowed) G-code text: how to open the complete file when a computed
    /// placeholder must scan the toolpath, and where to send diagnostics that only matter under --debug.
    /// </summary>
    internal sealed class ParseOptions(Func<Stream> openFullGcode, Action<string> debugLog)
    {
        public Func<Stream> OpenFullGcode { get; } = openFullGcode;
        public Action<string> DebugLog { get; } = debugLog;

        /// <summary>
        /// Options for G-code that is entirely in memory (tests, and the single-argument <c>ParseGcode</c> overload).
        /// </summary>
        public static ParseOptions InMemory(string gcode) =>
            new(() => new MemoryStream(Encoding.UTF8.GetBytes(gcode), writable: false), _ => { });
    }
}
