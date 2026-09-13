using Slic3rPostProcessingUploader.Services;
using System.Net;
using System.Text;

namespace Slic3rPostProcessingUploaderUnitTests.Services;

[TestClass]
public sealed class UpdateCheckServiceTests
{
    private const string LatestRelease = """{"tag_name":"v1.3.0","html_url":"https://github.com/HoffmanEngineering/Slic3rPostProcessingUploader/releases/tag/v1.3.0"}""";

    private StringWriter _screen = null!;
    private ConsoleOutput _output = null!;

    [TestInitialize]
    public void Setup()
    {
        _screen = new StringWriter();
        _output = new ConsoleOutput(_screen, null, verbose: false, unicode: true, useColor: false);
    }

    private Task<UpdateCheckService.Result?> Check(StubHttpMessageHandler handler, string currentVersion)
    {
        var client = new HttpClient(handler);
        return new UpdateCheckService(client, _output).CheckAsync(currentVersion);
    }

    [TestMethod]
    [DataRow("1.2.0", "v1.3.0", true)]
    [DataRow("1.2.0", "1.3.0", true)]
    [DataRow("1.2.0", "v1.2.1", true)]
    [DataRow("1.2.0", "v2.0.0", true)]
    [DataRow("1.3.0", "v1.3.0", false)]
    [DataRow("1.4.0", "v1.3.0", false)]
    [DataRow("1.3.0-beta.1", "v1.3.0", true)]
    [DataRow("1.3.0", "v1.3.0-beta.1", false)]
    [DataRow("1.3.0-beta.1", "v1.3.0-beta.2", true)]
    [DataRow("1.2.0.0", "v1.2.0", false)]
    public void IsNewer_ComparesSemanticVersions(string current, string latestTag, bool expected)
    {
        Assert.AreEqual(expected, UpdateCheckService.IsNewer(current, latestTag));
    }

    [TestMethod]
    [DataRow("0.0.0-dev", "v1.3.0")]
    [DataRow("Unknown", "v1.3.0")]
    [DataRow("1.2.0", "latest")]
    [DataRow("1.2.0", "")]
    public void IsNewer_IsFalseWhenEitherVersionCannotBeCompared(string current, string latestTag)
    {
        Assert.IsFalse(UpdateCheckService.IsNewer(current, latestTag));
    }

    [TestMethod]
    public async Task CheckAsync_RequestsTheLatestReleaseWithAUserAgent()
    {
        var handler = StubHttpMessageHandler.Respond(HttpStatusCode.OK, LatestRelease);

        await Check(handler, "1.2.0");

        Assert.IsNotNull(handler.Request);
        Assert.AreEqual(HttpMethod.Get, handler.Request.Method);
        Assert.AreEqual(UpdateCheckService.LatestReleaseUrl, handler.Request.RequestUri?.ToString());
        Assert.IsTrue(handler.Request.Headers.UserAgent.Count > 0, "GitHub rejects requests without a User-Agent");
    }

    [TestMethod]
    public async Task CheckAsync_WhenANewerReleaseExists_ReturnsIt()
    {
        var handler = StubHttpMessageHandler.Respond(HttpStatusCode.OK, LatestRelease);

        var result = await Check(handler, "1.2.0");

        Assert.IsNotNull(result);
        Assert.AreEqual("1.3.0", result.LatestVersion);
        Assert.AreEqual("https://github.com/HoffmanEngineering/Slic3rPostProcessingUploader/releases/tag/v1.3.0", result.ReleaseUrl);
    }

    [TestMethod]
    public async Task CheckAsync_WhenAlreadyUpToDate_ReturnsNull()
    {
        var handler = StubHttpMessageHandler.Respond(HttpStatusCode.OK, LatestRelease);

        Assert.IsNull(await Check(handler, "1.3.0"));
    }

    [TestMethod]
    public async Task CheckAsync_ForDevBuilds_DoesNotCallGitHub()
    {
        var handler = StubHttpMessageHandler.Respond(HttpStatusCode.OK, LatestRelease);

        Assert.IsNull(await Check(handler, "0.0.0-dev"));
        Assert.IsNull(handler.Request);
    }

    [TestMethod]
    public async Task CheckAsync_WhenTheReleaseHasNoHtmlUrl_FallsBackToTheReleasesPage()
    {
        var handler = StubHttpMessageHandler.Respond(HttpStatusCode.OK, """{"tag_name":"v1.3.0"}""");

        var result = await Check(handler, "1.2.0");

        Assert.IsNotNull(result);
        Assert.AreEqual(UpdateCheckService.ReleasesPageUrl, result.ReleaseUrl);
    }

    [TestMethod]
    public async Task CheckAsync_WhenGitHubIsUnreachable_ReturnsNullWithoutThrowing()
    {
        var handler = StubHttpMessageHandler.Throw(new HttpRequestException("No such host is known."));

        Assert.IsNull(await Check(handler, "1.2.0"));
        Assert.AreEqual(string.Empty, _screen.ToString(), "the check must stay silent on the screen");
    }

    [TestMethod]
    public async Task CheckAsync_WhenTheRequestTimesOut_ReturnsNull()
    {
        var handler = StubHttpMessageHandler.Throw(new TaskCanceledException("The request was canceled due to the configured HttpClient.Timeout"));

        Assert.IsNull(await Check(handler, "1.2.0"));
    }

    [TestMethod]
    [DataRow(HttpStatusCode.NotFound, """{"message":"Not Found"}""")]
    [DataRow(HttpStatusCode.Forbidden, """{"message":"API rate limit exceeded"}""")]
    [DataRow(HttpStatusCode.OK, "")]
    [DataRow(HttpStatusCode.OK, "not json")]
    [DataRow(HttpStatusCode.OK, """{"tag_name":""}""")]
    public async Task CheckAsync_WithAnUnusableResponse_ReturnsNull(HttpStatusCode statusCode, string body)
    {
        var handler = StubHttpMessageHandler.Respond(statusCode, body);

        Assert.IsNull(await Check(handler, "1.2.0"));
    }

    [TestMethod]
    public void Report_PrintsTheUpgradePromptAsAWarning()
    {
        var result = new UpdateCheckService.Result("1.3.0", "https://github.com/HoffmanEngineering/Slic3rPostProcessingUploader/releases/tag/v1.3.0");

        UpdateCheckService.Report(_output, result);

        Assert.AreEqual(
            "  ! A new version (v1.3.0) is available. Upgrade to get the latest features:" + Environment.NewLine +
            "    https://github.com/HoffmanEngineering/Slic3rPostProcessingUploader/releases/tag/v1.3.0" + Environment.NewLine,
            _screen.ToString());
    }

    private sealed class StubHttpMessageHandler : HttpMessageHandler
    {
        private readonly Func<HttpResponseMessage> _respond;

        public HttpRequestMessage? Request { get; private set; }

        private StubHttpMessageHandler(Func<HttpResponseMessage> respond) => _respond = respond;

        public static StubHttpMessageHandler Respond(HttpStatusCode statusCode, string body) =>
            new(() => new HttpResponseMessage(statusCode) { Content = new StringContent(body, Encoding.UTF8, "application/json") });

        public static StubHttpMessageHandler Throw(Exception exception) =>
            new(() => throw exception);

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Request = request;
            return Task.FromResult(_respond());
        }
    }
}
