using System.Globalization;
using System.Text;

namespace PrintLog.SlicerSettings
{
    /// <summary>
    /// The <c>file_name</c> and <c>print_name</c> rules, shared by the uploader (<c>PrintMetadata.Apply</c>) and
    /// <see cref="SlicerGcodeParser"/> so a print gets the same title whichever way its G-code arrives.
    /// </summary>
    internal static class PrintName
    {
        /// <summary>Titles are cut to this many characters.</summary>
        public const int MaxTitleLength = 100;

        /// <summary>
        /// The last segment of a path written on any OS: both '/' and '\' separate, so a Windows path reaching a Linux
        /// server gives the same file name it gives on the slicing machine.
        /// </summary>
        public static string FileName(string path) => path[(path.LastIndexOfAny(['/', '\\']) + 1)..];

        /// <summary>
        /// A print title from a file name without its extension: <c>benchy_final</c> and <c>BenchyFinal</c> both become
        /// <c>Benchy Final</c>. Underscores and capitals separate words, a <c>gcode</c> word is dropped, and the result is
        /// at most <see cref="MaxTitleLength"/> characters.
        /// </summary>
        public static string Title(string? fileNameWithoutExtension, CultureInfo culture)
        {
            if (string.IsNullOrEmpty(fileNameWithoutExtension))
            {
                return "";
            }

            string snakeCaseFilename = ToSnakeCase(fileNameWithoutExtension);
            string title = string.Join(" ", snakeCaseFilename.Split('_')
                .Where(segment => !segment.Equals("gcode", StringComparison.OrdinalIgnoreCase))
                .Select(culture.TextInfo.ToTitleCase))
                .Trim();

            return title.Length > MaxTitleLength ? title[..MaxTitleLength] : title;
        }

        public static string ToSnakeCase(string? text)
        {
            if (string.IsNullOrEmpty(text))
            {
                return "";
            }

            if (text.Length < 2)
            {
                return text.ToLowerInvariant();
            }

            StringBuilder sb = new();
            sb.Append(char.ToLowerInvariant(text[0]));
            for (int i = 1; i < text.Length; ++i)
            {
                char c = text[i];
                if (char.IsUpper(c))
                {
                    sb.Append('_');
                    sb.Append(char.ToLowerInvariant(c));
                }
                else
                {
                    sb.Append(c);
                }
            }
            return sb.ToString();
        }
    }
}
