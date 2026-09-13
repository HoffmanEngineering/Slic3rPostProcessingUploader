using Slic3rPostProcessingUploader.Services;
using Slic3rPostProcessingUploader.Services.Parsers;
using System.Collections;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;

[assembly: InternalsVisibleTo("Slic3rPostProcessingUploaderUnitTests")]

TelemetryService? telemetry = null;
ConsoleOutput output = ConsoleOutput.ForConsole(debugFile: null, verbose: false);
StreamWriter? debugFile = null;
int exitCode = 0;

try
{
    ArgumentParser arguments = new(args);

    string newPrintUrl = arguments.UseLocalDev ? "https://localhost:4200/prints/new/cura" : "https://www.3dprintlog.com/prints/new/cura";
    string apiUrl = arguments.UseLocalDev ? "https://localhost:5001/api/Cura/settings" : "https://api.3dprintlog.com/api/Cura/settings";

    telemetry = new TelemetryService(arguments.DisableTelemetry);

    // Track platform info
    telemetry.TrackEvent("Startup", new Dictionary<string, object> {
        { "OS", RuntimeInformation.OSDescription },
        { "Architecture", RuntimeInformation.OSArchitecture.ToString() },
        { "FrameworkVersion", RuntimeInformation.FrameworkDescription }
    });

    // Track CLI flags
    telemetry.TrackEvent("CLIFlags", new Dictionary<string, object> {
        { "UseDefaultTemplate", arguments.UseDefaultNoteTemplate },
        { "UseFullTemplate", arguments.UseFullNoteTemplate },
        { "UseCustomTemplate", !string.IsNullOrEmpty(arguments.NoteTemplatePath) },
        { "DebugEnabled", !string.IsNullOrEmpty(arguments.DebugPath) },
        { "LocalDev", arguments.UseLocalDev },
        { "TelemetryDisabled", arguments.DisableTelemetry }
    });

    if (arguments.DisplayHelp)
    {
        DisplayHelp(arguments);
        return 0;
    }

    if (arguments.DisplayVersion)
    {
        Console.WriteLine($"Slic3rPostProcessingUploader v{new VersionService().GetVersion()}");
        return 0;
    }

    debugFile = OpenDebugFile(arguments.DebugPath);
    output = ConsoleOutput.ForConsole(debugFile, verbose: debugFile != null);

    output.Header(new VersionService().GetVersion());
    if (!string.IsNullOrEmpty(arguments.DebugPath))
    {
        output.Info($"Debug output: {arguments.DebugPath}");
    }

    LogEnvironmentVariables(arguments.DebugPath);

    if (string.IsNullOrEmpty(arguments.InputFile))
    {
        throw new UserFacingException(
            "No G-code file was given.",
            "Add this program to your slicer's Post-Processing Scripts; the slicer passes the G-code path automatically.");
    }

    if (!File.Exists(arguments.InputFile))
    {
        throw new UserFacingException(
            $"G-code file not found: {arguments.InputFile}",
            "The slicer should pass the exported G-code path as the last argument.");
    }

    // Only the head and tail of the file carry slicer metadata, so that is all we read. Debug mode reads the whole file
    // so the full contents can be logged for troubleshooting.
    string fileContents = string.IsNullOrEmpty(arguments.DebugPath)
        ? GcodeWindow.ReadFromFile(arguments.InputFile)
        : File.ReadAllText(arguments.InputFile);
    LogFileContents(arguments.DebugPath, fileContents);

    IGcodeParser parser = ParserFactory.GetParser(arguments, telemetry, output, fileContents);

    // Track parse duration
    var parseStopwatch = Stopwatch.StartNew();
    CuraSettingDto dto = parser.ParseGcode(fileContents);
    parseStopwatch.Stop();

    var outputName = Environment.GetEnvironmentVariable("SLIC3R_PP_OUTPUT_NAME");
    dto.settings.file_name = outputName != null ? Path.GetFileName(outputName) : Path.GetFileName(arguments.InputFile);
    dto.settings.print_name = new TitleService().GetTitle(Path.GetFileNameWithoutExtension(dto.settings.file_name));
    dto.PluginVersion = new VersionService().GetVersion();

    output.Step($"Detected {dto.Slicer} {dto.CuraVersion}");
    output.Step($"Parsed {dto.settings.file_name} ({DescribeTemplate(arguments)} template, {parseStopwatch.ElapsedMilliseconds} ms)");

    telemetry.TrackEvent("Parse", new Dictionary<string, object> {
        { "Slicer", dto.Slicer },
        { "PluginVersion", dto.PluginVersion },
        { "CuraVersion", dto.CuraVersion },
        { "ParseDurationMs", parseStopwatch.ElapsedMilliseconds }
    });

    LogDto(arguments.DebugPath, dto);

    string settingId = await UploadToApi(telemetry, output, apiUrl, dto, arguments.DebugPath);
    output.Step("Uploaded print settings to 3dprintlog.com");

    string printUrl = $"{newPrintUrl}?cura_version={dto.CuraVersion}&plugin_version={dto.PluginVersion}&settingId={settingId}";
    output.Info($"Opening {printUrl}");
    OpenBrowser(output, printUrl);
}
catch (Exception e)
{
    output.ReportException(e);
    telemetry?.TrackException(e, "Main");
    exitCode = 1;
    ConsolePause.WaitForKeyOrTimeout(output, TimeSpan.FromSeconds(30));
}
finally
{
    telemetry?.Dispose();
    debugFile?.Dispose();
}

return exitCode;

void DisplayHelp(ArgumentParser arguments)
{
    arguments.DisplayHelpDocs();

    Console.WriteLine("Press any key to exit");
    Console.ReadKey();
}

static string DescribeTemplate(ArgumentParser arguments) =>
    arguments.UseDefaultNoteTemplate ? "default" : arguments.UseFullNoteTemplate ? "full" : "custom";

static StreamWriter? OpenDebugFile(string? debugPath)
{
    if (string.IsNullOrEmpty(debugPath))
    {
        return null;
    }

    try
    {
        Directory.CreateDirectory(debugPath);
        return new StreamWriter(Path.Combine(debugPath, "slic3r-debug.txt"), true) { AutoFlush = true };
    }
    catch (Exception e) when (e is IOException or UnauthorizedAccessException)
    {
        throw new UserFacingException(
            $"Could not create the debug folder: {debugPath}",
            "Check that the --debug path is writable.", e);
    }
}

void LogEnvironmentVariables(string? debugPath)
{
    if (!string.IsNullOrEmpty(debugPath))
    {
        IEnumerable<string> slic3rVariables = Environment.GetEnvironmentVariables()
            .Cast<DictionaryEntry>()
            .Where(x => x.Key.ToString()!.StartsWith("SLIC3R"))
            .ToDictionary(x => x.Key, x => x.Value)
            .Select(d => string.Format("\"{0}\": [{1}]", d.Key, string.Join(",", d.Value!)));

        string envVarFileName = "slic3r-environment-variables.json";
        string path = Path.Combine(debugPath, envVarFileName);
        File.WriteAllText(path, "{" + string.Join(",", slic3rVariables) + "}");
    }
}

void LogFileContents(string? debugPath, string fileContents)
{
    if (!string.IsNullOrEmpty(debugPath))
    {
        string slicerFileContentsFileName = "slic3r-file-contents.txt";
        string path = Path.Combine(debugPath, slicerFileContentsFileName);
        File.WriteAllText(path, fileContents);
    }
}

void LogDto(string? debugPath, CuraSettingDto dto)
{
    if (!string.IsNullOrEmpty(debugPath))
    {
        string dtoFileName = "slic3r-dto.json";
        string path = Path.Combine(debugPath, dtoFileName);
        File.WriteAllText(path, dto.ToJSON());
    }
}

// Posts the settings and returns the new setting id. Every failure surfaces as a UserFacingException.
async Task<string> UploadToApi(TelemetryService telemetry, ConsoleOutput output, string apiUrl, CuraSettingDto dto, string? debugPath)
{
    using HttpClient client = new();
    using StringContent content = new(dto.ToJSON(), Encoding.UTF8, "application/json");

    HttpResponseMessage response;
    try
    {
        response = await client.PostAsync(apiUrl, content);
    }
    catch (Exception e) when (e is HttpRequestException or TaskCanceledException)
    {
        telemetry.TrackEvent("UploadResult", new Dictionary<string, object> {
            { "Success", false },
            { "StatusCode", 0 },
            { "Reason", e.Message }
        });
        throw new UserFacingException(
            "Could not reach 3dprintlog.com.",
            "Check your internet connection and try again.", e);
    }

    output.Debug($"Response: {response}");
    string responseContent = await response.Content.ReadAsStringAsync();
    LogApiResponse(debugPath, responseContent);

    if (!response.IsSuccessStatusCode)
    {
        telemetry.TrackEvent("UploadResult", new Dictionary<string, object> {
            { "Success", false },
            { "StatusCode", (int)response.StatusCode },
            { "Reason", response.ReasonPhrase ?? "Unknown" }
        });

        string hint = (int)response.StatusCode >= 500
            ? "3dprintlog.com may be having trouble. Please try again in a few minutes."
            : $"Run again with --debug <folder> and report the issue at {ConsoleOutput.IssuesUrl}";
        throw new UserFacingException($"Upload to 3dprintlog.com failed ({(int)response.StatusCode} {response.ReasonPhrase}).", hint);
    }

    var apiResponse = JsonSerializer.Deserialize(responseContent, ApiResponseContext.Default.ApiResponse);
    if (apiResponse == null || string.IsNullOrEmpty(apiResponse.NewSettingId))
    {
        telemetry.TrackEvent("UploadResult", new Dictionary<string, object> {
            { "Success", false },
            { "StatusCode", (int)response.StatusCode },
            { "Reason", "Invalid API response: missing newSettingId" }
        });
        throw new UserFacingException(
            "3dprintlog.com returned an unexpected response.",
            $"Run again with --debug <folder> and report the issue at {ConsoleOutput.IssuesUrl}");
    }

    telemetry.TrackEvent("UploadResult", new Dictionary<string, object> {
        { "Success", true },
        { "StatusCode", (int)response.StatusCode }
    });

    return apiResponse.NewSettingId;
}

static void OpenBrowser(ConsoleOutput output, string url)
{
    try
    {
        new Browser(output).Open(url);
    }
    catch (Exception e)
    {
        throw new UserFacingException(
            "Could not open your web browser.",
            $"Open this link manually to finish logging the print:\n    {url}", e);
    }
}

void LogApiResponse(string? debugPath, string responseContent)
{
    if (!string.IsNullOrEmpty(debugPath))
    {
        string responseFileName = "3d-print-log-api-response.json";
        string path = Path.Combine(debugPath, responseFileName);
        File.WriteAllText(path, responseContent);
    }
}
