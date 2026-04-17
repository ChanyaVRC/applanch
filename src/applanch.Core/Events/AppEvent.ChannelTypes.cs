namespace applanch.Events;

public sealed partial class AppEvent
{
    private abstract class ChannelRegistration
    {
        internal abstract object CreateChannel(AppEvent appEvent);
    }

    private sealed class SignalChannelRegistration : ChannelRegistration
    {
        internal static SignalChannelRegistration Instance { get; } = new();

        internal override object CreateChannel(AppEvent appEvent)
            => new EventChannel(appEvent);
    }

    private sealed class SignalChannelRegistration<TPayload> : ChannelRegistration
    {
        internal static SignalChannelRegistration<TPayload> Instance { get; } = new();

        internal override object CreateChannel(AppEvent appEvent)
            => new EventChannel<TPayload>(appEvent);
    }

    private sealed class PayloadChannelRegistration : ChannelRegistration
    {
        private readonly InvokePipeline? _invokePipeline;

        internal PayloadChannelRegistration(InvokePipeline? invokePipeline)
        {
            _invokePipeline = invokePipeline;
        }

        internal override object CreateChannel(AppEvent appEvent)
            => new EventChannel(appEvent, _invokePipeline);
    }

    private sealed class PayloadChannelRegistration<TPayload> : ChannelRegistration
    {
        private readonly InvokePipeline<TPayload>? _invokePipeline;

        internal PayloadChannelRegistration(InvokePipeline<TPayload>? invokePipeline)
        {
            _invokePipeline = invokePipeline;
        }

        internal override object CreateChannel(AppEvent appEvent)
            => new EventChannel<TPayload>(appEvent, _invokePipeline);
    }

    private sealed class EventChannel
    {
        private readonly InvokePipeline _invokePipeline;
        private event Action? Handlers;
        private readonly AppEvent _appEvent;

        internal EventChannel(AppEvent appEvent, InvokePipeline? invokePipeline = null)
        {
            _appEvent = appEvent;
            _invokePipeline = invokePipeline ?? (static (_, next) => next());
        }

        internal void Register(Action handler) => Handlers += handler;

        internal void Unregister(Action handler) => Handlers -= handler;

        internal void Invoke() => _invokePipeline(_appEvent, InvokeHandlers);

        private void InvokeHandlers() => Handlers?.Invoke();
    }

    private sealed class EventChannel<TPayload>
    {
        private readonly AppEvent _appEvent;
        private readonly InvokePipeline<TPayload> _invokePipeline;
        private event Action<TPayload>? Handlers;

        internal EventChannel(
            AppEvent appEvent,
            InvokePipeline<TPayload>? invokePipeline = null)
        {
            _appEvent = appEvent;
            _invokePipeline = invokePipeline ?? (static (_, payload, next) => next(payload));
        }

        internal void Register(Action<TPayload> handler) => Handlers += handler;

        internal void Unregister(Action<TPayload> handler) => Handlers -= handler;

        internal void Invoke(TPayload payload)
            => _invokePipeline(_appEvent, payload, InvokeHandlers);

        private void InvokeHandlers(TPayload payload)
            => Handlers?.Invoke(payload);
    }
}
