using Slic3rPostProcessingUploader.Services.Parsers;
using System.Text;
using System.Text.Json;

namespace Slic3rPostProcessingUploader.Services;

/// <summary>
/// Posts the parsed settings to the 3D Print Log API. Every failure surfaces as a <see cref="UserFacingException"/>
/// so the top-level handler can show the user something actionable.
/// </summary>
internal sealed class UploadService
{
    // The payload is small JSON (the thumbnail is the only sizeable part), so a hung connection has no
    // legitimate reason to take anywhere near the 100 s default. Bound it so "Could not reach
    // 3dprintlog.com" shows up promptly instead of after nearly two minutes.
    public static readonly TimeSpan UploadTimeout = TimeSpan.FromSeconds(20);

    private readonly HttpClient _client;
    private readonly TelemetryService _telemetry;
    private readonly ConsoleOutput _output;

    public UploadService(HttpClient client, TelemetryService telemetry, ConsoleOutput output)
    {
        _client = client;
        _telemetry = telemetry;
        _output = output;
    }

    /// <summary>
    /// Posts <paramref name="dto"/> to <paramref name="apiUrl"/> and returns the new setting id.
    /// </summary>
    /// <param name="totalDurationMs">End-to-end wall time so far, reported with the success event.</param>
    public async Task<string> UploadAsync(string apiUrl, CuraSettingDto dto, string? debugPath, Func<long> totalDurationMs)
    {
        using StringContent content = new(dto.ToJSON(), Encoding.UTF8, "application/json");

        HttpResponseMessage response;
        try
        {
            response = await _client.PostAsync(apiUrl, content);
        }
        catch (Exception e) when (e is HttpRequestException or TaskCanceledException)
        {
            _telemetry.TrackEvent("UploadResult", new Dictionary<string, object> {
                { "Success", false },
                { "StatusCode", 0 },
                { "Reason", e.Message }
            });
            throw new UserFacingException(
                "Could not reach 3dprintlog.com.",
                "Check your internet connection and try again.", e);
        }

        using (response)
        {
            _output.Debug($"Response: {response}");
            string responseContent = await response.Content.ReadAsStringAsync();
            LogApiResponse(debugPath, responseContent);

            if (!response.IsSuccessStatusCode)
            {
                _telemetry.TrackEvent("UploadResult", new Dictionary<string, object> {
                    { "Success", false },
                    { "StatusCode", (int)response.StatusCode },
                    { "Reason", response.ReasonPhrase ?? "Unknown" }
                });

                string hint = (int)response.StatusCode >= 500
                    ? "3dprintlog.com may be having trouble. Please try again in a few minutes."
                    : $"Run again with --debug <folder> and report the issue at {ConsoleOutput.IssuesUrl}";
                throw new UserFacingException($"Upload to 3dprintlog.com failed ({(int)response.StatusCode} {response.ReasonPhrase}).", hint);
            }

            ApiResponse? apiResponse = TryDeserialize(responseContent);
            if (apiResponse == null || string.IsNullOrEmpty(apiResponse.NewSettingId))
            {
                _telemetry.TrackEvent("UploadResult", new Dictionary<string, object> {
                    { "Success", false },
                    { "StatusCode", (int)response.StatusCode },
                    { "Reason", "Invalid API response: missing newSettingId" }
                });
                throw new UserFacingException(
                    "3dprintlog.com returned an unexpected response.",
                    $"Run again with --debug <folder> and report the issue at {ConsoleOutput.IssuesUrl}");
            }

            _telemetry.TrackEvent("UploadResult", new Dictionary<string, object> {
                { "Success", true },
                { "StatusCode", (int)response.StatusCode },
                { "TotalDurationMs", totalDurationMs() }
            });

            return apiResponse.NewSettingId;
        }
    }

    // An empty or non-JSON 2xx body is just another "unexpected response"; it should not surface as a raw JsonException.
    private static ApiResponse? TryDeserialize(string responseContent)
    {
        if (string.IsNullOrWhiteSpace(responseContent))
        {
            return null;
        }

        try
        {
            return JsonSerializer.Deserialize(responseContent, ApiResponseContext.Default.ApiResponse);
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private static void LogApiResponse(string? debugPath, string responseContent)
    {
        if (!string.IsNullOrEmpty(debugPath))
        {
            string path = Path.Combine(debugPath, "3d-print-log-api-response.json");
            File.WriteAllText(path, responseContent);
        }
    }
}
