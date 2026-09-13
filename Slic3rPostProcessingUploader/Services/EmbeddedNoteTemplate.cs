using System.Reflection;

namespace Slic3rPostProcessingUploader.Services
{
    /// <summary>
    /// A built-in note template compiled into the executable from <c>Templates/{slicer}/{kind}.txt</c>.
    /// Line endings are normalised to LF and the file's trailing newline is dropped so the rendered note is
    /// identical regardless of how the template was checked out.
    /// </summary>
    internal sealed class EmbeddedNoteTemplate(string slicer, string kind) : INoteTemplate
    {
        public const string DefaultKind = "default";
        public const string FullKind = "full";

        private const string ResourcePrefix = "Templates/";

        public static EmbeddedNoteTemplate Default(string slicer) => new(slicer, DefaultKind);
        public static EmbeddedNoteTemplate Full(string slicer) => new(slicer, FullKind);

        public string ResourceName => $"{ResourcePrefix}{slicer}/{kind}.txt";

        public string getNoteTemplate()
        {
            using var stream = typeof(EmbeddedNoteTemplate).Assembly.GetManifestResourceStream(ResourceName)
                ?? throw new InvalidOperationException($"Built-in note template '{ResourceName}' is not embedded in the executable.");
            using var reader = new StreamReader(stream);
            return reader.ReadToEnd().ReplaceLineEndings("\n").TrimEnd('\n');
        }

        /// <summary>
        /// Every built-in template resource name, e.g. <c>Templates/OrcaSlicer/default.txt</c>.
        /// </summary>
        public static IEnumerable<string> ResourceNames() =>
            typeof(EmbeddedNoteTemplate).Assembly.GetManifestResourceNames()
                .Where(name => name.StartsWith(ResourcePrefix, StringComparison.Ordinal))
                .Order(StringComparer.Ordinal);
    }
}
