namespace Slic3rPostProcessingUploader.Services
{
    internal class NoteTemplateFromFile : INoteTemplate
    {
        private string filePath;

        public NoteTemplateFromFile(string filePath) {
            if (string.IsNullOrEmpty(filePath))
            {
                throw new ArgumentNullException("filePath");
            }

            this.filePath = filePath;
        }

        public string getNoteTemplate()
        {
            // Validate the filePath
            if (!System.IO.File.Exists(filePath))
            {
                throw new UserFacingException($"Note template not found: {filePath}", "Check the --template path in your slicer's post-processing script settings. Absolute paths work best.");
            }

            // Load the contents of the file from the filePath
            return System.IO.File.ReadAllText(filePath);
        }
    }
}
