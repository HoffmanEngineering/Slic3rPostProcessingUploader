using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Slic3rPostProcessingUploader.Services
{
    internal class Browser : IBrowser
    {
        private readonly ConsoleOutput _output;

        public Browser(ConsoleOutput output)
        {
            _output = output;
        }

        public void Open(string url)
        {
            try
            {
                Process.Start(url);
            }
            catch (Exception ex)
            {
                // hack because of this: https://github.com/dotnet/corefx/issues/10361
                _output.Debug($"Process.Start failed, trying platform-specific fallback: {ex.Message}");
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    url = url.Replace("&", "^&");
                    Process.Start(new ProcessStartInfo("cmd", $"/c start {url}") { CreateNoWindow = true });
                }
                else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                {
                    Process.Start("xdg-open", url);
                }
                else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                {
                    Process.Start("open", url);
                }
                else
                {
                    throw;
                }
            }
        }
    }
}
