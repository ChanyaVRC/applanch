namespace applanch.Infrastructure.Storage;

internal static class AppSettingsProvider
{
    internal static AppSettings Current { get; private set; } = new();

    internal static AppSettings Load()
    {
        Current = AppSettings.Load();
        return Current;
    }

    internal static AppSettings Save(AppSettings settings)
    {
        var normalized = settings.Normalize();
        normalized.Save();
        Current = normalized;
        return Current;
    }

    internal static AppSettings ApplyCurrent(AppSettings settings)
    {
        Current = settings.Normalize();
        return Current;
    }
}
