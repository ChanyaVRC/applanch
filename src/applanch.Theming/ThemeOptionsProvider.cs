namespace applanch.Theming;

public static class ThemeOptionsProvider
{
    public static IReadOnlyDictionary<string, ThemeOption> Load()
    {
        try
        {
            return BuildOptions(ThemePaletteConfigurationLoader.Load());
        }
        catch (InvalidOperationException)
        {
            return new Dictionary<string, ThemeOption>();
        }
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
