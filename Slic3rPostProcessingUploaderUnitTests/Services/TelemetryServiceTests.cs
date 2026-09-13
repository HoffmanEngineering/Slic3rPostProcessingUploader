using Slic3rPostProcessingUploader.Services;
using System.Diagnostics;

namespace Slic3rPostProcessingUploaderUnitTests.Services
{
    [TestClass]
    public class TelemetryServiceTests
    {
        [TestMethod]
        public void Dispose_WhenTelemetryDisabled_ReturnsPromptly()
        {
            using var telemetry = new TelemetryService(disableTelemetry: true);

            var stopwatch = Stopwatch.StartNew();
            telemetry.Dispose();
            stopwatch.Stop();

            // A disabled service never built an OpenTelemetry pipeline, so Dispose (which flushes it)
            // has nothing to wait on. This guards against a future change accidentally routing a
            // disabled TelemetryService through the flush/dispose path anyway.
            Assert.IsTrue(stopwatch.ElapsedMilliseconds < 1000, $"Dispose took {stopwatch.ElapsedMilliseconds} ms, expected it to return promptly when telemetry is disabled.");
        }

        [TestMethod]
        public void TrackEvent_WhenTelemetryDisabled_DoesNotThrow()
        {
            using var telemetry = new TelemetryService(disableTelemetry: true);

            telemetry.TrackEvent("Startup", new Dictionary<string, object> { { "OS", "test" } });
            telemetry.TrackEvent("NoProperties");
        }

        [TestMethod]
        public void TrackException_WhenTelemetryDisabled_DoesNotThrow()
        {
            using var telemetry = new TelemetryService(disableTelemetry: true);

            telemetry.TrackException(new InvalidOperationException("test"), "UnitTest");
        }

        [TestMethod]
        public void Flush_WhenTelemetryDisabled_ReturnsPromptly()
        {
            using var telemetry = new TelemetryService(disableTelemetry: true);

            var stopwatch = Stopwatch.StartNew();
            telemetry.Flush();
            stopwatch.Stop();

            Assert.IsTrue(stopwatch.ElapsedMilliseconds < 1000, $"Flush took {stopwatch.ElapsedMilliseconds} ms, expected it to return promptly when telemetry is disabled.");
        }
    }
}
