using System.Diagnostics;
using applanch.Events;

namespace applanch.Infrastructure.Storage;

internal static class AppSettingsProvider
{
    private static AppEvent? _registeredEvent;
    private static bool _isLoaded;

    internal static AppSettings Current
    {
        get
        {
            if (!_isLoaded)
            {
                SetCurrent(AppSettings.Load());
            }

            return field;
        }
        private set => field = value;
    } = new();

    internal static AppSettings Load() => Current;

    internal static void Register(AppEvent appEvent)
    {
        ArgumentNullException.ThrowIfNull(appEvent);
        Debug.Assert(_registeredEvent == null);

        _registeredEvent = appEvent;
        _registeredEvent.Register(AppEvents.Refresh, OnRefresh);
    }

    internal static void Unregister(AppEvent appEvent)
    {
        Debug.Assert(_registeredEvent == appEvent);

        _registeredEvent.Unregister(AppEvents.Refresh, OnRefresh);
        _registeredEvent = null;
    }

    private static void OnRefresh(AppRefreshPayload payload)
    {
        SetCurrent(payload.CurrentSettings);
    }

    private static void SetCurrent(AppSettings settings)
    {
        Current = settings.Normalize();
        _isLoaded = true;
    }
}
