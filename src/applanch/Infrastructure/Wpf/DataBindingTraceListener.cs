using System.Diagnostics;
using applanch.Core.Utilities;

namespace applanch.Infrastructure.Wpf;

internal sealed class DataBindingTraceListener : TraceListener
{
    private readonly AppLogger _logger;

    internal DataBindingTraceListener(AppLogger logger)
    {
        _logger = logger;
    }

    public override void Write(string? message)
    {
        // DataBinding trace messages are handled in TraceEvent/TraceData where event type is available.
    }

    public override void WriteLine(string? message)
    {
        // Ignore untyped writes to avoid noisy logs.
    }

    public override void TraceEvent(TraceEventCache? eventCache, string? source, TraceEventType eventType, int id, string? message)
    {
        if (!ShouldLog(eventType))
        {
            return;
        }

        _logger.Warn($"DataBinding trace ({eventType}): {NormalizeMessage(message)}");
    }

    public override void TraceData(TraceEventCache? eventCache, string? source, TraceEventType eventType, int id, object? data)
    {
        if (!ShouldLog(eventType))
        {
            return;
        }

        _logger.Warn($"DataBinding trace ({eventType}): {NormalizeMessage(data?.ToString())}");
    }

    public override void TraceData(TraceEventCache? eventCache, string? source, TraceEventType eventType, int id, params object?[]? data)
    {
        if (!ShouldLog(eventType))
        {
            return;
        }

        if (data is null || data.Length == 0)
        {
            _logger.Warn($"DataBinding trace ({eventType}): <empty>");
            return;
        }

        var text = string.Join(" | ", data.Select(static value => NormalizeMessage(value?.ToString())));
        _logger.Warn($"DataBinding trace ({eventType}): {text}");
    }

    internal static bool ShouldLog(TraceEventType eventType)
    {
        return eventType is TraceEventType.Warning or TraceEventType.Error or TraceEventType.Critical;
    }

    internal static string NormalizeMessage(string? message)
    {
        if (string.IsNullOrWhiteSpace(message))
        {
            return "<empty>";
        }

        return message.Trim();
    }
}