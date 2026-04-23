using System.Text.Json.Serialization;

namespace applanch.Theming;

/// <summary>
/// Intermediate DTO for deserializing theme-palette.json.
/// Maps directly to the JSON structure.
/// </summary>
public sealed record ThemePaletteConfigurationDto(
    [property: JsonPropertyName("themes")]
    IReadOnlyList<ThemeDto> Themes);

