using applanch.Settings;
using applanch.Infrastructure.Updates;
using applanch.Theming;
using applanch.Updates;
using applanch.ViewModels;

namespace applanch.Tests.ViewModels.TestDoubles;

internal sealed class SettingsWindowViewModelTestRuntime : ISettingsWindowViewModelRuntime
{
    public Func<IReadOnlyDictionary<string, ThemeOption>> ThemeOptionsMapProvider { get; init; } = ThemeOptionsProvider.Load;
    public Func<AppSettings, IAppUpdateService> UpdateServiceFactory { get; init; } = static settings => new GitHubAppUpdateService(settings.DebugUpdate, settings.AllowPrereleaseUpdates);

    public IReadOnlyDictionary<string, ThemeOption> LoadThemeOptions() => ThemeOptionsMapProvider();

    public IAppUpdateService CreateUpdateService(AppSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);
        return UpdateServiceFactory(settings);
    }
}
