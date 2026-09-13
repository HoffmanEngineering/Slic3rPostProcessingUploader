using Slic3rPostProcessingUploader.Services;
using Slic3rPostProcessingUploader.Services.Parsers;
using System.Net;
using System.Text;

namespace Slic3rPostProcessingUploaderUnitTests.Services;

[TestClass]
public sealed class UploadServiceTests
{
    private const string ApiUrl = "https://api.example.test/api/Cura/settings";
    private const string ReportHint = "Run again with --debug <folder> and report the issue at " + ConsoleOutput.IssuesUrl;

    private StringWriter _screen = null!;
    private ConsoleOutput _output = null!;
    private TelemetryService _telemetry = null!;

    [TestInitialize]
    public void Setup()
    {
        _screen = new StringWriter();
        _output = new ConsoleOutput(_screen, null, verbose: false, unicode: true, useColor: false);
        _telemetry = new TelemetryService(disableTelemetry: true);
    }

    [TestCleanup]
    public void Cleanup() => _telemetry.Dispose();

    private static CuraSettingDto SampleDto() => new()
    {
        Slicer = "OrcaSlicer",
        CuraVersion = "2.2.0",
        PluginVersion = "1.0.0.0",
        settings = new CuraSettings { note = "Settings:", print_name = "Cube", file_name = "cube.gcode" },
    };

    private Task<string> Upload(StubHttpMessageHandler handler, string? debugPath = null)
    {
        var client = new HttpClient(handler);
        return new UploadService(client, _telemetry, _output).UploadAsync(ApiUrl, SampleDto(), debugPath, () => 0);
    }

    private static async Task<UserFacingException> AssertThrowsUserFacing(Task<string> upload, string expectedMessage, string expectedHint)
    {
        var exception = await Assert.ThrowsExceptionAsync<UserFacingException>(() => upload);
        Assert.AreEqual(expectedMessage, exception.Message);
        Assert.AreEqual(expectedHint, exception.Hint);
        return exception;
    }

    [TestMethod]
    public async Task UploadAsync_PostsCamelCaseJsonToTheApiUrl()
    {
        var handler = StubHttpMessageHandler.Respond(HttpStatusCode.OK, """{"newSettingId":"abc-123"}""");

        await Upload(handler);

        Assert.IsNotNull(handler.Request);
        Assert.AreEqual(HttpMethod.Post, handler.Request.Method);
        Assert.AreEqual(ApiUrl, handler.Request.RequestUri?.ToString());
        Assert.AreEqual("application/json", handler.Request.Content?.Headers.ContentType?.MediaType);
        Assert.AreEqual(SampleDto().ToJSON(), handler.RequestBody);
    }

    [TestMethod]
    public async Task UploadAsync_WithValidNewSettingId_ReturnsIt()
    {
        var handler = StubHttpMessageHandler.Respond(HttpStatusCode.OK, """{"newSettingId":"abc-123"}""");

        var settingId = await Upload(handler);

        Assert.AreEqual("abc-123", settingId);
    }

    [TestMethod]
    public async Task UploadAsync_WhenRequestFails_ReportsUnreachable()
    {
        var cause = new HttpRequestException("No such host is known.");
        var handler = StubHttpMessageHandler.Throw(cause);

        var exception = await AssertThrowsUserFacing(Upload(handler),
            "Could not reach 3dprintlog.com.",
            "Check your internet connection and try again.");

        Assert.AreSame(cause, exception.InnerException);
    }

    [TestMethod]
    public async Task UploadAsync_WhenRequestTimesOut_ReportsUnreachable()
    {
        var cause = new TaskCanceledException("The request was canceled due to the configured HttpClient.Timeout of 20 seconds elapsing.");
        var handler = StubHttpMessageHandler.Throw(cause);

        var exception = await AssertThrowsUserFacing(Upload(handler),
            "Could not reach 3dprintlog.com.",
            "Check your internet connection and try again.");

        Assert.AreSame(cause, exception.InnerException);
    }

    [TestMethod]
    public async Task UploadAsync_With400_AsksUserToReportTheIssue()
    {
        var handler = StubHttpMessageHandler.Respond(HttpStatusCode.BadRequest, """{"error":"bad"}""");

        await AssertThrowsUserFacing(Upload(handler),
            "Upload to 3dprintlog.com failed (400 Bad Request).",
            ReportHint);
    }

    [TestMethod]
    public async Task UploadAsync_With500_SuggestsRetryingLater()
    {
        var handler = StubHttpMessageHandler.Respond(HttpStatusCode.InternalServerError, "");

        await AssertThrowsUserFacing(Upload(handler),
            "Upload to 3dprintlog.com failed (500 Internal Server Error).",
            "3dprintlog.com may be having trouble. Please try again in a few minutes.");
    }

    [TestMethod]
    public async Task UploadAsync_With200AndEmptyBody_ReportsUnexpectedResponse()
    {
        var handler = StubHttpMessageHandler.Respond(HttpStatusCode.OK, "");

        await AssertThrowsUserFacing(Upload(handler),
            "3dprintlog.com returned an unexpected response.",
            ReportHint);
    }

    [TestMethod]
    public async Task UploadAsync_With200AndEmptyObject_ReportsUnexpectedResponse()
    {
        var handler = StubHttpMessageHandler.Respond(HttpStatusCode.OK, "{}");

        await AssertThrowsUserFacing(Upload(handler),
            "3dprintlog.com returned an unexpected response.",
            ReportHint);
    }

    [TestMethod]
    public async Task UploadAsync_With200AndNonJsonBody_ReportsUnexpectedResponse()
    {
        var handler = StubHttpMessageHandler.Respond(HttpStatusCode.OK, "<html>maintenance</html>");

        await AssertThrowsUserFacing(Upload(handler),
            "3dprintlog.com returned an unexpected response.",
            ReportHint);
    }

    [TestMethod]
    public async Task UploadAsync_WithDebugPath_WritesTheRawApiResponse()
    {
        string debugPath = Path.Combine(Path.GetTempPath(), "UploadServiceTests-" + Guid.NewGuid());
        Directory.CreateDirectory(debugPath);
        try
        {
            var handler = StubHttpMessageHandler.Respond(HttpStatusCode.OK, """{"newSettingId":"abc-123"}""");

            await Upload(handler, debugPath);

            Assert.AreEqual("""{"newSettingId":"abc-123"}""", File.ReadAllText(Path.Combine(debugPath, "3d-print-log-api-response.json")));
        }
        finally
        {
            Directory.Delete(debugPath, recursive: true);
        }
    }

    /// <summary>
    /// Replaces the network: either answers every request with a fixed response or throws before one is sent.
    /// </summary>
    private sealed class StubHttpMessageHandler : HttpMessageHandler
    {
        private readonly Func<HttpResponseMessage> _respond;

        public HttpRequestMessage? Request { get; private set; }
        public string? RequestBody { get; private set; }

        private StubHttpMessageHandler(Func<HttpResponseMessage> respond) => _respond = respond;

        public static StubHttpMessageHandler Respond(HttpStatusCode statusCode, string body) =>
            new(() => new HttpResponseMessage(statusCode) { Content = new StringContent(body, Encoding.UTF8, "application/json") });

        public static StubHttpMessageHandler Throw(Exception exception) =>
            new(() => throw exception);

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Request = request;
            // Read the body now: the service disposes the StringContent as soon as the call returns.
            RequestBody = request.Content == null ? null : await request.Content.ReadAsStringAsync(cancellationToken);
            return _respond();
        }
    }
}
