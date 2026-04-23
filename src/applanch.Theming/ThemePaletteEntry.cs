namespace applanch.Theming;

internal sealed record ThemePaletteEntry(
    ThemeBrushKey Key,
    IReadOnlyDictionary<string, ThemeColor> ColorsByThemeId);
