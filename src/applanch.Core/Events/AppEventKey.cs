namespace applanch.Events;

public sealed class AppEventKey<TPayload>(AppEventType type)
{
    public AppEventType Type { get; } = type;

    public string PayloadName { get; } = typeof(TPayload).Name;
}
