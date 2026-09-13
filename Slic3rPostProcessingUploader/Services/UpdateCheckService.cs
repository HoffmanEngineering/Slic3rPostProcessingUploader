using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Slic3rPostProcessingUploader.Services;

/// <summary>
/// Asks GitHub for the latest release and tells the user when it is newer than the running build. The check is
/// purely advisory: it never throws, never writes to the screen on its own, and a dev build (<c>0.0.0-dev</c>)
/// skips the request entirely because it would always look outdated.
/// </summary>
internal sealed class UpdateCheckService
{
    public const string ReleasesPageUrl = "https://github.com/HoffmanEngineering/Slic3rPostProcessingUploader/releases";
    public const string LatestReleaseUrl = "https://api.github.com/repos/HoffmanEngineering/Slic3rPostProcessingUploader/releases/latest";

    // The request is started before parsing and only awaited after the upload, so this rarely adds any wall time.
    // It is only a bound on how long a slow GitHub can hold the slicer's "post-processing" step open.
    public static readonly TimeSpan CheckTimeout = TimeSpan.FromSeconds(5);

    public sealed record Result(string LatestVersion, string ReleaseUrl);

    private readonly HttpClient _client;
    private readonly ConsoleOutput _output;

    public UpdateCheckService(HttpClient client, ConsoleOutput output)
    {
        _client = client;
        _output = output;
    }

    /// <summary>
    /// Returns the latest release when it is newer than <paramref name="currentVersion"/>, otherwise <c>null</c>.
    /// Failures (offline, rate limited, odd payload) are logged to the debug output and also yield <c>null</c>.
    /// </summary>
    public async Task<Result?> CheckAsync(string currentVersion)
    {
        if (!TryParse(currentVersion, out _))
        {
            _output.Debug($"Update check skipped: current version '{currentVersion}' is not a release build");
            return null;
        }

        try
        {
            using HttpRequestMessage request = new(HttpMethod.Get, LatestReleaseUrl);
            // GitHub's API rejects anonymous requests that do not identify themselves.
            request.Headers.UserAgent.ParseAdd($"Slic3rPostProcessingUploader/{currentVersion}");
            request.Headers.Accept.ParseAdd("application/vnd.github+json");

            using HttpResponseMessage response = await _client.SendAsync(request);
            if (!response.IsSuccessStatusCode)
            {
                _output.Debug($"Update check failed: GitHub returned {(int)response.StatusCode} {response.ReasonPhrase}");
                return null;
            }

            string body = await response.Content.ReadAsStringAsync();
            GitHubRelease? release = TryDeserialize(body);
            if (release == null || string.IsNullOrWhiteSpace(release.TagName))
            {
                _output.Debug("Update check failed: GitHub response did not include a tag_name");
                return null;
            }

            _output.Debug($"Update check: running {currentVersion}, latest release is {release.TagName}");
            if (!IsNewer(currentVersion, release.TagName))
            {
                return null;
            }

            string url = string.IsNullOrWhiteSpace(release.HtmlUrl) ? ReleasesPageUrl : release.HtmlUrl;
            return new Result(release.TagName.TrimStart('v', 'V'), url);
        }
        catch (Exception e) when (e is HttpRequestException or TaskCanceledException)
        {
            _output.Debug($"Update check failed: {e.Message}");
            return null;
        }
    }

    public static void Report(ConsoleOutput output, Result result)
    {
        output.Warn($"A new version (v{result.LatestVersion}) is available. Upgrade to get the latest features:");
        output.Raw($"    {result.ReleaseUrl}");
    }

    /// <summary>
    /// <c>true</c> when <paramref name="latestTag"/> (a release tag such as <c>v1.3.0</c>) is a higher version than
    /// <paramref name="currentVersion"/>. Anything that is not a version, including the <c>0.0.0-dev</c> placeholder,
    /// compares as not newer so an unexpected value can never trigger a false upgrade prompt.
    /// </summary>
    internal static bool IsNewer(string currentVersion, string latestTag) =>
        TryParse(currentVersion, out var current) && TryParse(latestTag, out var latest) && latest.CompareTo(current) > 0;

    private static bool TryParse(string? text, [NotNullWhen(true)] out SemanticVersion? version)
    {
        version = null;
        if (string.IsNullOrWhiteSpace(text))
        {
            return false;
        }

        text = text.Trim().TrimStart('v', 'V');
        int dash = text.IndexOf('-');
        string numbers = dash >= 0 ? text[..dash] : text;
        string prerelease = dash >= 0 ? text[(dash + 1)..] : string.Empty;

        // Version.TryParse accepts two to four numeric parts; the placeholder 0.0.0 is not a real release.
        if (!Version.TryParse(numbers, out Version? parsed) || (parsed.Major == 0 && parsed.Minor == 0 && parsed.Build <= 0))
        {
            return false;
        }

        version = new SemanticVersion(new Version(parsed.Major, parsed.Minor, Math.Max(parsed.Build, 0), Math.Max(parsed.Revision, 0)), prerelease);
        return true;
    }

    /// <summary>
    /// Numeric parts first; then a release outranks its own prereleases, and prereleases order by their label.
    /// </summary>
    private sealed record SemanticVersion(Version Numbers, string Prerelease) : IComparable<SemanticVersion>
    {
        public int CompareTo(SemanticVersion? other)
        {
            if (other == null) return 1;

            int numbers = Numbers.CompareTo(other.Numbers);
            if (numbers != 0) return numbers;

            if (Prerelease.Length == 0) return other.Prerelease.Length == 0 ? 0 : 1;
            if (other.Prerelease.Length == 0) return -1;
            return string.CompareOrdinal(Prerelease, other.Prerelease);
        }
    }

    private static GitHubRelease? TryDeserialize(string body)
    {
        if (string.IsNullOrWhiteSpace(body))
        {
            return null;
        }

        try
        {
            return JsonSerializer.Deserialize(body, GitHubReleaseContext.Default.GitHubRelease);
        }
        catch (JsonException)
        {
            return null;
        }
    }
}

internal sealed class GitHubRelease
{
    [JsonPropertyName("tag_name")]
    public string? TagName { get; set; }

    [JsonPropertyName("html_url")]
    public string? HtmlUrl { get; set; }
}

[JsonSerializable(typeof(GitHubRelease))]
internal partial class GitHubReleaseContext : JsonSerializerContext
{
}
