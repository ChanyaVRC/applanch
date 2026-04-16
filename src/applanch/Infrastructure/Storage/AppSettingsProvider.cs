using applanch.Events;

namespace applanch.Infrastructure.Storage;

internal static class AppSettingsProvider
{
    private static AppEvent? _registeredEvent;
    private static bool _isLoaded;

    internal static AppSettings Current { get; private set; } = new();

    internal static AppSettings Load()
    {
        if (_isLoaded)
        {
            return Current;
        }

        Current = AppSettings.Load();
        _isLoaded = true;
        return Current;
    }

    internal static void Register(AppEvent appEvent)
    {
        ArgumentNullException.ThrowIfNull(appEvent);

        if (ReferenceEquals(_registeredEvent, appEvent))
        {
            return;
        }

        if (_registeredEvent is not null)
        {
            _registeredEvent.Unregister(AppEvents.Refresh, OnRefresh);
        }

        _registeredEvent = appEvent;
        _registeredEvent.Register(AppEvents.Refresh, OnRefresh);
    }

    internal static void Unregister(AppEvent appEvent)
    {
        if (!ReferenceEquals(_registeredEvent, appEvent))
        {
            return;
        }

        _registeredEvent.Unregister(AppEvents.Refresh, OnRefresh);
        _registeredEvent = null;
    }

    internal static AppSettings ApplyCurrent(AppSettings settings)
    {
        Current = settings.Normalize();
        _isLoaded = true;
        return Current;
    }

    private static void OnRefresh(AppRefreshPayload payload)
    {
        ApplyCurrent(payload.CurrentSettings);
    }
}
