using System.Globalization;
using PrintLog.SlicerSettings;

namespace Slic3rPostProcessingUploader.Services
{
    /// <summary>
    /// The print title rules, which live in the library (<see cref="PrintName"/>) so the API's G-code uploads title a
    /// print the same way. The uploader titles in the user's culture.
    /// </summary>
    internal class TitleService
    {
        public string GetTitle(string? filename) => PrintName.Title(filename, CultureInfo.CurrentCulture);

        public string ToSnakeCase(string? text) => PrintName.ToSnakeCase(text);
    }
}
