namespace applanch;

internal sealed class AppSignalEventKey(AppEventType type)
{
    internal AppEventType Type { get; } = type;
}