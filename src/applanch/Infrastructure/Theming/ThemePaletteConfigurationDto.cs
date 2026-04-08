namespace applanch.Infrastructure.Theming;

/// <summary>
/// Intermediate DTO for deserializing theme-palette.json.
/// Maps directly to the JSON structure.
/// </summary>
internal sealed record ThemePaletteConfigurationDto(
    IReadOnlyList<ThemeDto> Themes);

internal sealed record ThemeDto(
    string Id,
    Dictionary<string, string>? DisplayNames = null,
    System.Text.Json.JsonElement? EntriesFrom = null,
    IReadOnlyList<ThemeEntryDto>? Entries = null,
    bool Disabled = false);

internal sealed record ThemeEntryDto(
    string Key,
    string Hex);

