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
        var visibleThemes = configuration.Themes
            .Where(static x => x.IsVisibleInThemeList)
            .ToArray();

        if (visibleThemes.Length == 0)
        {
            return [];
        }

        return visibleThemes
            .Select(static x => new ThemeOption(
                x.Id,
                x.DisplayName,
                IsSystemOption: x.Id == ThemePaletteConfigurationLoader.SystemThemeId))
            .ToList();
    }
}
