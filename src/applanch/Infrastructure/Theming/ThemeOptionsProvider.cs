namespace applanch.Infrastructure.Theming;

internal static class ThemeOptionsProvider
{
    internal static IReadOnlyDictionary<string, ThemeOption> Load()
    {
        if (!ThemePaletteConfigurationLoader.TryLoadForSettings(out var configuration))
        {
            return new Dictionary<string, ThemeOption>();
        }

        return BuildOptions(configuration);
    }

    internal static IReadOnlyDictionary<string, ThemeOption> BuildOptions(ThemePaletteConfiguration configuration)
    {
        return configuration.Themes
            .Where(static x => x.IsVisibleInThemeList)
            .ToDictionary(
                static x => x.Id,
                static x => new ThemeOption(
                    x.Id,
                    x.DisplayName,
                    IsSystemOption: x.Id == ThemePaletteConfigurationLoader.SystemThemeId));
    }
}
