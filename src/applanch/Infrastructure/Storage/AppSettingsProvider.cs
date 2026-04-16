using System.Diagnostics;
using applanch.Events;

namespace applanch.Infrastructure.Storage;

internal static class AppSettingsProvider
{
    private static AppEvent? _registeredEvent;

    internal static AppSettings Current
    {
        get => field ??= AppSettings.Load();
        private set => field = value;
    }

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
        Current = payload.CurrentSettings.Normalize();
    }
}
