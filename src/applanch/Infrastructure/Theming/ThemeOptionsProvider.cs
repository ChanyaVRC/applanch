namespace applanch.Infrastructure.Theming;

internal static class ThemeOptionsProvider
{
    internal static IReadOnlyList<ThemeOption> Load()
    {
        if (!ThemePaletteConfigurationLoader.TryLoadForSettings(out var configuration))
        {
            return [];
        }

        var options = new List<ThemeOption>(configuration.Themes.Count + 1)
        {
            new ThemeOption(
                ThemePaletteConfigurationLoader.SystemThemeId,
                AppResources.Theme_System,
                IsSystemOption: true)
        };

        options.AddRange(configuration.Themes.Select(static x => new ThemeOption(x.Id, x.DisplayName.ResolveCurrentCulture())));
        return options;
    }
}
