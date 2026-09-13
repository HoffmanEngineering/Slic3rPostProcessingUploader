using Azure.Monitor.OpenTelemetry.Exporter;
using Microsoft.Extensions.Logging;
using OpenTelemetry;
using OpenTelemetry.Logs;
using OpenTelemetry.Trace;

namespace Slic3rPostProcessingUploader.Services;

internal sealed class TelemetryService : IDisposable
{
    private const string ServiceName = "Slic3rPostProcessingUploader";
    private const string ConnectionString = "InstrumentationKey=44698ebf-3363-4d89-b83d-5a0a616b22f5;IngestionEndpoint=https://eastus-8.in.applicationinsights.azure.com/;LiveEndpoint=https://eastus.livediagnostics.monitor.azure.com/;ApplicationId=ff0f0688-8e7b-4a59-b446-2969a28faae2";

    // The slicer blocks on this process's exit, and the browser tab (the user-visible result) is
    // already open by the time we get here. Any telemetry still sitting in memory at that point is a
    // "best effort" send: give it a couple of seconds to leave the machine, but never make the user's
    // wait for the app to close depend on Application Insights being reachable.
    private const int FlushTimeoutMilliseconds = 2000;

    // OpenTelemetrySdk.Create() wires logging and tracing off a single builder and hands back one
    // object exposing both providers, so both pipelines can be force-flushed (and disposed) together
    // within FlushTimeoutMilliseconds. Building the two pipelines independently (as before, via
    // LoggerFactory.Create + Sdk.CreateTracerProviderBuilder) leaves the logger side with no supported
    // way to force a flush before Dispose, so events emitted right before exit could be dropped.
    private readonly OpenTelemetrySdk? _sdk;
    private readonly ILogger? _logger;
    private readonly bool _isEnabled;

    public TelemetryService(bool disableTelemetry = false)
    {
        _isEnabled = !disableTelemetry;

        if (_isEnabled)
        {
            _sdk = OpenTelemetrySdk.Create(builder => builder
                // Custom events (TrackEvent/TrackException) go through the logging pipeline.
                .WithLogging(logging => logging.AddAzureMonitorLogExporter(exporterOptions =>
                {
                    exporterOptions.ConnectionString = ConnectionString;
                }))
                // HTTP dependency tracking (the upload to 3dprintlog.com) goes through tracing.
                .WithTracing(tracing => tracing
                    .AddSource(ServiceName)
                    .AddHttpClientInstrumentation()
                    .AddAzureMonitorTraceExporter(exporterOptions =>
                    {
                        exporterOptions.ConnectionString = ConnectionString;
                    })));

            _logger = _sdk.GetLoggerFactory().CreateLogger(ServiceName);
        }
    }

    public void TrackEvent(string eventName, Dictionary<string, object>? properties = null)
    {
        if (!_isEnabled || _logger == null) return;

        if (properties != null && properties.Count > 0)
        {
            var state = properties.Select(p => new KeyValuePair<string, object?>(p.Key, p.Value)).ToList();
            state.Add(new KeyValuePair<string, object?>("EventName", eventName));

            _logger.Log(LogLevel.Information, 0, state, null, (s, _) => eventName);
        }
        else
        {
            _logger.LogInformation("{EventName}", eventName);
        }
    }

    public void TrackException(Exception exception, string? context = null)
    {
        if (!_isEnabled || _logger == null) return;

        var state = new List<KeyValuePair<string, object?>>
        {
            new("EventName", "Exception"),
            new("ExceptionType", exception.GetType().Name),
            new("ExceptionMessage", exception.Message),
            new("StackTrace", exception.StackTrace ?? "")
        };

        if (!string.IsNullOrEmpty(context))
        {
            state.Add(new("Context", context));
        }

        _logger.Log(LogLevel.Error, 0, state, exception, (s, ex) => $"Exception: {ex?.Message}");
    }

    public void Flush(int timeoutMilliseconds = FlushTimeoutMilliseconds)
    {
        if (_sdk == null) return;

        // Flush both pipelines within the same bounded budget; neither call blocks past its own timeout.
        _sdk.LoggerProvider.ForceFlush(timeoutMilliseconds);
        _sdk.TracerProvider.ForceFlush(timeoutMilliseconds);
    }

    public void Dispose()
    {
        Flush();
        _sdk?.Dispose();
    }
}
