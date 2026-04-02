namespace applanch.Infrastructure.Theming;

internal static class ThemeOptionsProvider
{
    internal static IReadOnlyList<ThemeOption> Load()
    {
        if (!ThemePaletteConfigurationLoader.TryLoadForSettings(out var configuration))
        {
            return [];
        }

        return BuildOptions(configuration);
    }

    internal static IReadOnlyList<ThemeOption> BuildOptions(ThemePaletteConfiguration configuration)
    {
        if (configuration.Themes.Count == 0)
        {
            return [];
        }

        var hasSystemTheme = configuration.Themes.Any(static x =>
            string.Equals(x.Id, ThemePaletteConfigurationLoader.SystemThemeId, StringComparison.OrdinalIgnoreCase));
        var options = new List<ThemeOption>(configuration.Themes.Count + (hasSystemTheme ? 0 : 1));

        if (!hasSystemTheme)
        {
            options.Add(new ThemeOption(
                ThemePaletteConfigurationLoader.SystemThemeId,
                AppResources.Theme_System,
                IsSystemOption: true));
        }

        options.AddRange(configuration.Themes.Select(static x => new ThemeOption(
            x.Id,
            x.DisplayName.ResolveCurrentCulture(),
            IsSystemOption: string.Equals(x.Id, ThemePaletteConfigurationLoader.SystemThemeId, StringComparison.OrdinalIgnoreCase))));

        return options;
    }
}
