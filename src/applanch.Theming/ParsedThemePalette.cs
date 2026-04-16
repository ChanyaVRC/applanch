namespace applanch.Theming;

internal sealed record ParsedThemePalette(
    ThemeDefinition[] Themes,
    ThemePaletteEntry[] Entries);
