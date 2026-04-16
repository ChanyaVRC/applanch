using System.Text.Json.Serialization;
using applanch.Core.Localization;

namespace applanch.Theming;

/// <summary>
/// Intermediate DTO for deserializing theme-palette.json.
/// Maps directly to the JSON structure.
/// </summary>
internal sealed record ThemePaletteConfigurationDto(
    IReadOnlyList<ThemeDto> Themes);

internal sealed record ThemeDto(
    string Id,
    [property: JsonConverter(typeof(LocalizedTextJsonConverter))] LocalizedText? DisplayNames = null,
    EntriesFromSpec? EntriesFrom = null,
    IReadOnlyList<ThemeEntryDto>? Entries = null,
    bool Enabled = true);

internal sealed record ThemeEntryDto(
    string Key,
    [property: JsonConverter(typeof(ThemeColorJsonConverter))] ThemeColor Hex);
