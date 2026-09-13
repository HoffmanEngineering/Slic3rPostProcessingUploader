using Slic3rPostProcessingUploader.Services;

namespace Slic3rPostProcessingUploaderUnitTests.Services
{
    [TestClass]
    public class NoteTemplateFromFileTests
    {
        [TestMethod]
        public void GetNoteTemplate_MissingFile_ThrowsUserFacingExceptionNamingThePath()
        {
            var path = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N") + ".txt");

            var ex = Assert.ThrowsException<UserFacingException>(() => new NoteTemplateFromFile(path).getNoteTemplate());

            StringAssert.Contains(ex.Message, path);
            Assert.IsNotNull(ex.Hint);
        }

        [TestMethod]
        public void GetNoteTemplate_ExistingFile_ReturnsContents()
        {
            var path = Path.GetTempFileName();
            try
            {
                File.WriteAllText(path, "{{layer_height}}");

                Assert.AreEqual("{{layer_height}}", new NoteTemplateFromFile(path).getNoteTemplate());
            }
            finally
            {
                File.Delete(path);
            }
        }
    }
}
