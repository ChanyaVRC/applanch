namespace applanch.Events;

internal sealed class AppSignalEventKey(AppEventType type)
{
    internal AppEventType Type { get; } = type;
}