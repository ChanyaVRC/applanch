namespace applanch.Events;

public sealed partial class AppEvent
{
    private static readonly Lazy<AppEvent> InstanceFactory =
        new(() => new AppEvent());
    private static readonly Lock RegistrationSync = new();
    private static readonly Dictionary<int, ChannelRegistration> Registrations = [];

    public static AppEvent Instance => InstanceFactory.Value;

    private static int _nextKeyId;

    private readonly Dictionary<int, object> _channels;

    private AppEvent()
    {
        _channels = [];
    }

    public static AppEventKey<TPayload> Register<TPayload>(string name)
        => Register<TPayload>(name, SignalChannelRegistration<TPayload>.Instance);

    public static AppEventKey<TPayload> Register<TPayload>(string name, InvokePipeline<TPayload>? invokePipeline = null)
        => Register<TPayload>(name, new PayloadChannelRegistration<TPayload>(invokePipeline));

    private static AppEventKey<TPayload> Register<TPayload>(string name, ChannelRegistration registration)
    {
        var eventKey = CreateKey<TPayload>(name);
        RegisterChannel(eventKey.Id, eventKey.Name, registration);
        return eventKey;
    }

    public static AppEventKey Register(string name)
        => Register(name, SignalChannelRegistration.Instance);

    public static AppEventKey Register(string name, InvokePipeline invokePipeline)
        => Register(name, new PayloadChannelRegistration(invokePipeline));

    private static AppEventKey Register(string name, ChannelRegistration registration)
    {
        var eventKey = CreateKey(name);
        RegisterChannel(eventKey.Id, eventKey.Name, registration);
        return eventKey;
    }


    public void Subscribe<TPayload>(AppEventKey<TPayload> eventKey, Action<TPayload> handler)
        => GetChannel(eventKey).Register(handler);

    public void Unsubscribe<TPayload>(AppEventKey<TPayload> eventKey, Action<TPayload> handler)
        => GetChannel(eventKey).Unregister(handler);

    public void Invoke<TPayload>(AppEventKey<TPayload> eventKey, TPayload payload)
        => GetChannel(eventKey).Invoke(payload);

    public void Subscribe(AppEventKey eventKey, Action handler)
        => GetSignalChannel(eventKey).Register(handler);

    public void Unsubscribe(AppEventKey eventKey, Action handler)
        => GetSignalChannel(eventKey).Unregister(handler);

    public void Invoke(AppEventKey eventKey)
        => GetSignalChannel(eventKey).Invoke();

    private EventChannel<TPayload> GetChannel<TPayload>(AppEventKey<TPayload> eventKey)
    {
        var channel = GetChannelOrThrow(eventKey.Id, nameof(eventKey), eventKey.Name);

        if (channel is not EventChannel<TPayload> typedChannel)
        {
            throw new ArgumentException($"Event key {GetKeyLabel(eventKey.Name, eventKey.Id)} expects payload {GetPayloadName(channel)}; received {eventKey.PayloadName}.", nameof(eventKey));
        }

        return typedChannel;
    }

    private EventChannel GetSignalChannel(AppEventKey eventKey)
    {
        var channel = GetChannelOrThrow(eventKey.Id, nameof(eventKey), eventKey.Name);

        if (channel is not EventChannel noPayloadChannel)
        {
            throw new ArgumentException($"Event key {GetKeyLabel(eventKey.Name, eventKey.Id)} expects payload {GetPayloadName(channel)}.", nameof(eventKey));
        }

        return noPayloadChannel;
    }

    private object GetChannelOrThrow(int eventId, string parameterName, string? name)
    {
        if (_channels.TryGetValue(eventId, out var channel))
        {
            return channel;
        }

        channel = GetRegistrationOrThrow(eventId, parameterName, name).CreateChannel(this);
        _channels[eventId] = channel;
        return channel;
    }

    private static ChannelRegistration GetRegistrationOrThrow(int eventId, string parameterName, string? name)
    {
        lock (RegistrationSync)
        {
            if (Registrations.TryGetValue(eventId, out var registration))
            {
                return registration;
            }
        }

        throw new ArgumentException($"Unknown event key: {GetKeyLabel(name, eventId)}", parameterName);
    }

    private static void RegisterChannel(int eventId, string? name, ChannelRegistration registration)
    {
        lock (RegistrationSync)
        {
            if (!Registrations.TryAdd(eventId, registration))
            {
                throw new InvalidOperationException($"Event key {GetKeyLabel(name, eventId)} is already registered.");
            }
        }
    }

    private static AppEventKey<TPayload> CreateKey<TPayload>(string? name)
    {
        var id = Interlocked.Increment(ref _nextKeyId);
        return new AppEventKey<TPayload>(id, name);
    }

    private static AppEventKey CreateKey(string? name)
    {
        var id = Interlocked.Increment(ref _nextKeyId);
        return new AppEventKey(id, name);
    }

    private static string GetKeyLabel(string? name, int id)
        => string.IsNullOrWhiteSpace(name) ? id.ToString() : $"{name} ({id})";

    private static string GetPayloadName(object channel)
    {
        var channelType = channel.GetType();
        return channelType.IsGenericType
            ? channelType.GenericTypeArguments[0].Name
            : "no payload";
    }
}
