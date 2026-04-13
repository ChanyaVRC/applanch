namespace applanch.Infrastructure.Theming;

internal sealed class ThemePaletteConfiguration
{
    internal ThemePaletteConfiguration(IEnumerable<ThemeDefinition> themes)
    {
        Themes = themes.ToArray();
        Entries = BuildEntries(Themes);
    }

    internal ThemePaletteConfiguration(
        IEnumerable<ThemeDefinition> themes,
        IEnumerable<ThemePaletteEntry> entries)
        : this(ApplyEntries(themes, entries))
    {
    }

    internal IReadOnlyList<ThemeDefinition> Themes { get; }

    internal IReadOnlyList<ThemePaletteEntry> Entries { get; }

    private static IEnumerable<ThemeDefinition> ApplyEntries(
        IEnumerable<ThemeDefinition> themes,
        IEnumerable<ThemePaletteEntry> entries)
    {
        var colorsByThemeId = new Dictionary<string, Dictionary<string, ThemeColor>>();

        foreach (var entry in entries)
        {
            foreach (var (themeId, hex) in entry.ColorsByThemeId)
            {
                if (!colorsByThemeId.TryGetValue(themeId, out var colorsByKey))
                {
                    colorsByKey = new Dictionary<string, ThemeColor>();
                    colorsByThemeId[themeId] = colorsByKey;
                }

                colorsByKey[entry.Key] = hex;
            }
        }

        return themes
            .Select(theme => colorsByThemeId.TryGetValue(theme.Id, out var colorsByKey)
                ? ApplyColors(theme, colorsByKey)
                : theme);
    }

    private static ThemeDefinition ApplyColors(
        ThemeDefinition theme,
        IReadOnlyDictionary<string, ThemeColor> colorsByKey)
    {
        return theme switch
        {
            FixedThemeDefinition fixedTheme => new FixedThemeDefinition(
                fixedTheme.Id,
                fixedTheme.DisplayName,
                fixedTheme.InheritedThemeId,
                new Dictionary<string, ThemeColor>(colorsByKey),
                fixedTheme.IsVisibleInThemeList),
            _ => theme,
        };
    }

    private static List<ThemePaletteEntry> BuildEntries(IEnumerable<ThemeDefinition> themes)
    {
        var colorsByEntryKey = new Dictionary<string, Dictionary<string, ThemeColor>>();

        foreach (var theme in themes)
        {
            foreach (var (key, hex) in theme.ColorsByKey)
            {
                if (!colorsByEntryKey.TryGetValue(key, out var colorsByThemeId))
                {
                    colorsByThemeId = new Dictionary<string, ThemeColor>();
                    colorsByEntryKey[key] = colorsByThemeId;
                }

                colorsByThemeId[theme.Id] = hex;
            }
        }

        var entries = new List<ThemePaletteEntry>(colorsByEntryKey.Count);
        foreach (var (entryKey, colorsByThemeId) in colorsByEntryKey)
        {
            entries.Add(new ThemePaletteEntry(entryKey, colorsByThemeId));
        }

        return entries;
    }
}
