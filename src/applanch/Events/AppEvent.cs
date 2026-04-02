using applanch.Infrastructure.Storage;
using applanch.Infrastructure.Updates;

namespace applanch.Events;

internal sealed class AppEvent
{
    private readonly Dictionary<AppEventType, object> _channels = new()
    {
        [AppEventType.Commit] = new EventChannel<AppSettings>(),
        [AppEventType.Refresh] = new EventChannel<AppSettings>(),
        [AppEventType.UpdateCheckRequested] = new EventChannel(),
        [AppEventType.UpdateAvailabilityChanged] = new EventChannel<AppUpdateInfo?>(),
    };

    internal void Register<TPayload>(AppEventKey<TPayload> eventKey, Action<TPayload> handler)
        => GetChannel(eventKey).Register(handler);

    internal void Unregister<TPayload>(AppEventKey<TPayload> eventKey, Action<TPayload> handler)
        => GetChannel(eventKey).Unregister(handler);

    internal void Invoke<TPayload>(AppEventKey<TPayload> eventKey, TPayload payload)
        => GetChannel(eventKey).Invoke(payload);

    internal void Register(AppSignalEventKey eventKey, Action handler)
        => GetSignalChannel(eventKey).Register(handler);

    internal void Unregister(AppSignalEventKey eventKey, Action handler)
        => GetSignalChannel(eventKey).Unregister(handler);

    internal void Invoke(AppSignalEventKey eventKey)
        => GetSignalChannel(eventKey).Invoke();

    private EventChannel<TPayload> GetChannel<TPayload>(AppEventKey<TPayload> eventKey)
    {
        if (!_channels.TryGetValue(eventKey.Type, out var channel))
        {
            throw new ArgumentException($"Unknown event type: {eventKey.Type}", nameof(eventKey));
        }

        if (channel is not EventChannel<TPayload> typedChannel)
        {
            throw new ArgumentException($"Event type {eventKey.Type} expects payload {GetPayloadName(channel)}; received {eventKey.PayloadName}.", nameof(eventKey));
        }

        return typedChannel;
    }

    private EventChannel GetSignalChannel(AppSignalEventKey eventKey)
    {
        if (!_channels.TryGetValue(eventKey.Type, out var channel))
        {
            throw new ArgumentException($"Unknown event type: {eventKey.Type}", nameof(eventKey));
        }

        if (channel is not EventChannel noPayloadChannel)
        {
            throw new ArgumentException($"Event type {eventKey.Type} expects payload {GetPayloadName(channel)}.", nameof(eventKey));
        }

        return noPayloadChannel;
    }

    private static string GetPayloadName(object channel)
    {
        var channelType = channel.GetType();
        return channelType.IsGenericType
            ? channelType.GenericTypeArguments[0].Name
            : "no payload";
    }

    private sealed class EventChannel
    {
        private event Action? Handlers;

        internal void Register(Action handler) => Handlers += handler;

        internal void Unregister(Action handler) => Handlers -= handler;

        internal void Invoke() => Handlers?.Invoke();
    }

    private sealed class EventChannel<TPayload>
    {
        private event Action<TPayload>? Handlers;

        internal void Register(Action<TPayload> handler) => Handlers += handler;

        internal void Unregister(Action<TPayload> handler) => Handlers -= handler;

        internal void Invoke(TPayload payload) => Handlers?.Invoke(payload);
    }
}