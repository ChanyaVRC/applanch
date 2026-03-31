namespace applanch;

internal sealed class AppEventKey<TPayload>(AppEventType type)
{
    internal AppEventType Type { get; } = type;

    internal string PayloadName { get; } = typeof(TPayload).Name;
}