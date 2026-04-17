using applanch.Settings;
using applanch.Theming;
using applanch.Updates;

namespace applanch.ViewModels;

internal interface ISettingsWindowViewModelRuntime
{
    IReadOnlyDictionary<string, ThemeOption> LoadThemeOptions();

    IAppUpdateService CreateUpdateService(AppSettings settings);
}
