using System.Diagnostics;

namespace Slic3rPostProcessingUploader.Services
{
    internal static class Browser
    {
        /// <summary>
        /// Opens <paramref name="url"/> in the user's default browser. <c>UseShellExecute</c> hands the URL to the OS
        /// (ShellExecute on Windows, <c>open</c> on macOS, <c>xdg-open</c> on Linux), so no per-platform shelling out is needed.
        /// </summary>
        public static void Open(string url)
        {
            Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
        }
    }
}
