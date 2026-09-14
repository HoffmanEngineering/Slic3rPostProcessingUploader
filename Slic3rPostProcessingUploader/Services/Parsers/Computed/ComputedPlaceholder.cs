namespace Slic3rPostProcessingUploader.Services.Parsers.Computed
{
    /// <summary>
    /// A template key whose value is produced by code instead of being read from a "; key = value" line. A parser lists
    /// the placeholders it supports; each one runs only when the active note template references its key, so a template
    /// that does not use it costs nothing. <paramref name="Render"/> must never throw: it returns an empty string (or a
    /// degraded value) and reports the reason through <see cref="ComputedContext.DebugLog"/>.
    /// </summary>
    internal sealed record ComputedPlaceholder(string Key, Func<ComputedContext, string> Render);

    /// <summary>
    /// What a computed placeholder can draw on: the indexed settings and, for values that need the toolpath, a way to
    /// stream the whole G-code (the parser itself only sees the head and tail of large files).
    /// </summary>
    internal sealed class ComputedContext(GcodeSettings settings, ParseOptions options)
    {
        public GcodeSettings Settings { get; } = settings;
        public Func<Stream> OpenFullGcode => options.OpenFullGcode;
        public Action<string> DebugLog => options.DebugLog;
    }
}
