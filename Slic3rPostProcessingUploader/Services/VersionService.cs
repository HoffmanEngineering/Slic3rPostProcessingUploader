using System.Reflection;

namespace Slic3rPostProcessingUploader.Services
{
    internal class VersionService
    {
        public string GetVersion()
        {
            var assembly = Assembly.GetExecutingAssembly();
            return Format(
                assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion,
                assembly.GetName().Version);
        }

        /// <summary>
        /// Prefers the informational version because it is the csproj <c>Version</c> verbatim
        /// (<c>1.2.0</c>, <c>1.2.0-beta.1</c>), whereas <see cref="AssemblyName.Version"/> is always
        /// padded to four parts and drops any prerelease label. The SDK appends <c>+&lt;commit&gt;</c>
        /// when it can find a source revision; that is stripped so the output stays a plain version.
        /// </summary>
        internal static string Format(string? informationalVersion, Version? assemblyVersion)
        {
            if (!string.IsNullOrWhiteSpace(informationalVersion))
            {
                int metadata = informationalVersion.IndexOf('+');
                return metadata >= 0 ? informationalVersion[..metadata] : informationalVersion;
            }

            return assemblyVersion?.ToString() ?? "Unknown";
        }
    }
}
