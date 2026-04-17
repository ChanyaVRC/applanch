using applanch.Settings;

namespace applanch.Infrastructure.Storage;

public static class AppSettingsProvider
{
    public static AppSettings Current
    {
        get => field ??= AppSettings.Load();
        private set => field = value;
    }

    internal static AppSettings NormalizeAndSetCurrent(AppSettings settings)
    {
        var normalized = settings.Normalize();
        Current = normalized;
        return normalized;
    }
}