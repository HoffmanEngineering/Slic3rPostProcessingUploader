using Slic3rPostProcessingUploader.Services;

namespace Slic3rPostProcessingUploaderUnitTests.Services
{
    [TestClass]
    public sealed class EmbeddedNoteTemplateTests
    {
        private static readonly string[] Slicers = ["OrcaSlicer", "PrusaSlicer", "BambuStudio", "FLSunSlicer", "AnycubicSlicerNext"];

        [TestMethod]
        public void EmbedsADefaultAndFullTemplateForEverySlicer()
        {
            var expected = Slicers
                .SelectMany(slicer => new[] { $"Templates/{slicer}/default.txt", $"Templates/{slicer}/full.txt" })
                .Order(StringComparer.Ordinal);

            CollectionAssert.AreEqual(expected.ToList(), EmbeddedNoteTemplate.ResourceNames().ToList());
        }

        public static IEnumerable<object[]> Templates =>
            Slicers.SelectMany(slicer => new[] { EmbeddedNoteTemplate.DefaultKind, EmbeddedNoteTemplate.FullKind }.Select(kind => new object[] { slicer, kind }));

        [TestMethod]
        [DynamicData(nameof(Templates))]
        public void LoadsTemplateWithLfLineEndingsAndNoTrailingNewline(string slicer, string kind)
        {
            var template = new EmbeddedNoteTemplate(slicer, kind).getNoteTemplate();

            Assert.IsTrue(template.StartsWith("Settings:\n", StringComparison.Ordinal), $"{slicer}/{kind} should start with the Settings: heading");
            Assert.IsFalse(template.Contains('\r'), $"{slicer}/{kind} should be normalised to LF");
            Assert.IsFalse(template.EndsWith('\n'), $"{slicer}/{kind} should not end with a newline");
            Assert.IsTrue(template.Contains("{{layer_height}}", StringComparison.Ordinal), $"{slicer}/{kind} should contain placeholders");
        }

        [TestMethod]
        public void ThrowsWhenTemplateIsNotEmbedded()
        {
            var ex = Assert.ThrowsException<InvalidOperationException>(() => new EmbeddedNoteTemplate("NoSuchSlicer", "default").getNoteTemplate());

            StringAssert.Contains(ex.Message, "Templates/NoSuchSlicer/default.txt");
        }
    }
}
