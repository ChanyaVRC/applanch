namespace applanch.Events;

public sealed class AppSignalEventKey(AppEventType type)
{
    public AppEventType Type { get; } = type;
}
