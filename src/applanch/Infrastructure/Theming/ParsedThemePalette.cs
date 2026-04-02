namespace applanch.Infrastructure.Theming;

internal sealed record ParsedThemePalette(
    ThemeDefinition[] Themes,
    ThemePaletteEntry[] Entries);
