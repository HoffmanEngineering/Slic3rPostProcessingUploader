using System.Reflection;

namespace Slic3rPostProcessingUploader.Services
{
    internal class VersionService
    {
        public string GetVersion()
        {
            var version = Assembly.GetExecutingAssembly().GetName().Version;
            return version != null ? version.ToString() : "Unknown";
        }
    }
}
