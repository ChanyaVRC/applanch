namespace applanch.Infrastructure.Theming;

internal sealed record ThemePaletteConfiguration(
    IReadOnlyList<ThemeDefinition> Themes,
    IReadOnlyList<ThemePaletteEntry> Entries,
    bool LoadedFromConfig);
