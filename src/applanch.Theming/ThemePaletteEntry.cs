namespace applanch.Theming;

internal sealed record ThemePaletteEntry(
    string Key,
    IReadOnlyDictionary<string, ThemeColor> ColorsByThemeId);
