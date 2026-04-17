using applanch.Settings;
using applanch.Infrastructure.Updates;
using applanch.Theming;
using applanch.Updates;

namespace applanch.ViewModels;

internal sealed class SettingsWindowViewModelRuntime : ISettingsWindowViewModelRuntime
{
    private readonly Func<AppSettings, IAppUpdateService> _updateServiceFactory;

    internal SettingsWindowViewModelRuntime()
        : this(static settings => new GitHubAppUpdateService(settings.DebugUpdate, settings.AllowPrereleaseUpdates))
    {
    }

    internal SettingsWindowViewModelRuntime(Func<AppSettings, IAppUpdateService> updateServiceFactory)
    {
        _updateServiceFactory = updateServiceFactory;
    }

    public IReadOnlyDictionary<string, ThemeOption> LoadThemeOptions() => ThemeOptionsProvider.Load();

    public IAppUpdateService CreateUpdateService(AppSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);
        return _updateServiceFactory(settings);
    }
}
