using System.Text;

namespace Slic3rPostProcessingUploader.Services.Parsers
{
    /// <summary>
    /// Per-run inputs a parser needs beyond the (windowed) G-code text: how to open the complete file when a computed
    /// placeholder must scan the toolpath, where to send diagnostics that only matter under --debug, and which slicer
    /// config directories on this machine may be consulted for the user's own preset files.
    /// </summary>
    internal sealed class ParseOptions(Func<Stream> openFullGcode, Action<string> debugLog, Func<IEnumerable<string>>? configRoots = null)
    {
        public Func<Stream> OpenFullGcode { get; } = openFullGcode;
        public Action<string> DebugLog { get; } = debugLog;

        /// <summary>
        /// Slicer config roots to search for user presets. Resolved lazily; empty means "do not touch the disk".
        /// </summary>
        public Func<IEnumerable<string>> ConfigRoots { get; } = configRoots ?? (() => []);

        /// <summary>
        /// Options for G-code that is entirely in memory and nothing else: no file access, no config directories.
        /// Used by tests and the single-argument <c>ParseGcode</c> overload, so both are hermetic.
        /// </summary>
        public static ParseOptions InMemory(string gcode) =>
            new(() => new MemoryStream(Encoding.UTF8.GetBytes(gcode), writable: false), _ => { });
    }
}
