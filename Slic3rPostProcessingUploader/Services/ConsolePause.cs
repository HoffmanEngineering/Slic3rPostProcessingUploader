namespace Slic3rPostProcessingUploader.Services;

/// <summary>
/// Keeps the console window open after an error so the user can read it. Slicers launch this program in a window
/// that closes on exit, but they may also capture stdin, so this never blocks on ReadKey.
/// </summary>
internal static class ConsolePause
{
    public static void WaitForKeyOrTimeout(ConsoleOutput output, TimeSpan timeout)
    {
        if (Console.IsInputRedirected || Console.IsOutputRedirected)
        {
            return;
        }

        output.Info($"Press any key to close (closes automatically in {(int)timeout.TotalSeconds}s)");

        var deadline = DateTime.UtcNow + timeout;
        while (DateTime.UtcNow < deadline)
        {
            if (Console.KeyAvailable)
            {
                Console.ReadKey(intercept: true);
                return;
            }
            Thread.Sleep(100);
        }
    }
}
