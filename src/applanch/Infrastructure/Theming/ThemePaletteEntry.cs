namespace applanch.Infrastructure.Theming;

internal sealed record ThemePaletteEntry(
    string Key,
    IReadOnlyDictionary<string, string> ColorsByThemeId);
