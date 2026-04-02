namespace applanch.Infrastructure.Theming;

internal sealed class ThemePaletteConfiguration
{
    internal ThemePaletteConfiguration(
        IReadOnlyList<ThemeDefinition> themes,
        bool LoadedFromConfig)
    {
        Themes = themes.ToArray();
        Entries = BuildEntries(Themes);
        this.LoadedFromConfig = LoadedFromConfig;
    }

    internal ThemePaletteConfiguration(
        IReadOnlyList<ThemeDefinition> themes,
        IReadOnlyList<ThemePaletteEntry> entries,
        bool LoadedFromConfig)
        : this(ApplyEntries(themes, entries), LoadedFromConfig)
    {
    }

    internal IReadOnlyList<ThemeDefinition> Themes { get; }

    internal IReadOnlyList<ThemePaletteEntry> Entries { get; }

    internal bool LoadedFromConfig { get; }

    private static ThemeDefinition[] ApplyEntries(
        IReadOnlyList<ThemeDefinition> themes,
        IReadOnlyList<ThemePaletteEntry> entries)
    {
        var colorsByThemeId = new Dictionary<string, Dictionary<string, string>>();

        foreach (var entry in entries)
        {
            foreach (var (themeId, hex) in entry.ColorsByThemeId)
            {
                if (!colorsByThemeId.TryGetValue(themeId, out var colorsByKey))
                {
                    colorsByKey = new Dictionary<string, string>();
                    colorsByThemeId[themeId] = colorsByKey;
                }

                colorsByKey[entry.Key] = hex;
            }
        }

        return themes
            .Select(theme => colorsByThemeId.TryGetValue(theme.Id, out var colorsByKey)
                ? ApplyColors(theme, colorsByKey)
                : theme)
            .ToArray();
    }

    private static ThemeDefinition ApplyColors(
        ThemeDefinition theme,
        IReadOnlyDictionary<string, string> colorsByKey)
    {
        return theme switch
        {
            FixedThemeDefinition fixedTheme => new FixedThemeDefinition(
                fixedTheme.Id,
                fixedTheme.DisplayName,
                fixedTheme.InheritedThemeId,
                new Dictionary<string, string>(colorsByKey)),
            _ => theme,
        };
    }

    private static ThemePaletteEntry[] BuildEntries(IReadOnlyList<ThemeDefinition> themes)
    {
        var colorsByEntryKey = new Dictionary<string, Dictionary<string, string>>();

        foreach (var theme in themes)
        {
            foreach (var (key, hex) in theme.ColorsByKey)
            {
                if (!colorsByEntryKey.TryGetValue(key, out var colorsByThemeId))
                {
                    colorsByThemeId = new Dictionary<string, string>();
                    colorsByEntryKey[key] = colorsByThemeId;
                }

                colorsByThemeId[theme.Id] = hex;
            }
        }

        return colorsByEntryKey
            .Select(static entry => new ThemePaletteEntry(entry.Key, entry.Value))
            .ToArray();
    }
}
