using System.Text;

namespace Slic3rPostProcessingUploader.Services;

/// <summary>
/// User-facing console output. Every line is mirrored to the debug file (when one is open);
/// <see cref="Debug"/> lines only reach the screen when running with --debug.
/// </summary>
internal class ConsoleOutput
{
    private readonly TextWriter _screen;
    private readonly TextWriter? _debugFile;
    private readonly bool _verbose;
    private readonly bool _useColor;

    private readonly string _ok;
    private readonly string _arrow;
    private readonly string _warn;
    private readonly string _cross;

    public ConsoleOutput(TextWriter screen, TextWriter? debugFile, bool verbose, bool unicode, bool useColor)
    {
        _screen = screen;
        _debugFile = debugFile;
        _verbose = verbose;
        _useColor = useColor;

        _ok = unicode ? "✓" : "[OK]";
        _arrow = unicode ? "→" : "->";
        _warn = "!";
        _cross = unicode ? "✗" : "[X]";
    }

    /// <summary>
    /// Builds the output for the real console: UTF-8 glyphs and color only when a human is looking at a terminal.
    /// </summary>
    public static ConsoleOutput ForConsole(TextWriter? debugFile, bool verbose)
    {
        bool interactive = !Console.IsOutputRedirected;
        if (interactive && OperatingSystem.IsWindows())
        {
            Console.OutputEncoding = Encoding.UTF8;
        }

        return new ConsoleOutput(Console.Out, debugFile, verbose, unicode: interactive, useColor: interactive);
    }

    public void Header(string version) => WriteLine($"3D Print Log Uploader v{version}", ConsoleColor.Cyan);

    public void Step(string message) => WriteLine($"  {_ok} {message}", ConsoleColor.Green);

    public void Info(string message) => WriteLine($"  {_arrow} {message}", null);

    /// <summary>
    /// Writes text verbatim (no indent, no glyph, no color). Used for multi-line payloads such as a rendered note.
    /// </summary>
    public void Raw(string text) => WriteLine(text, null);

    public void Warn(string message) => WriteLine($"  {_warn} {message}", ConsoleColor.Yellow);

    public void Error(string message, string? hint = null)
    {
        WriteLine($"  {_cross} {message}", ConsoleColor.Red);
        if (!string.IsNullOrEmpty(hint))
        {
            WriteLine($"    {hint}", null);
        }
    }

    public const string IssuesUrl = "https://github.com/HoffmanEngineering/Slic3rPostProcessingUploader/issues";

    /// <summary>
    /// Prints a user-facing error for the exception. Full exception details only go to the debug output.
    /// </summary>
    public void ReportException(Exception e)
    {
        if (e is UserFacingException ufe)
        {
            Error(ufe.Message, ufe.Hint);
        }
        else
        {
            Error($"Something went wrong: {e.Message}", $"Run again with --debug <folder> and report the issue at {IssuesUrl}");
        }

        Debug(e.ToString());
    }

    public void Debug(string message)
    {
        var line = $"    {message}";
        if (_verbose)
        {
            WriteLine(line, ConsoleColor.DarkGray);
        }
        else
        {
            _debugFile?.WriteLine(line);
        }
    }

    private void WriteLine(string line, ConsoleColor? color)
    {
        if (_useColor && color.HasValue)
        {
            Console.ForegroundColor = color.Value;
            _screen.WriteLine(line);
            Console.ResetColor();
        }
        else
        {
            _screen.WriteLine(line);
        }

        _debugFile?.WriteLine(line);
    }
}
