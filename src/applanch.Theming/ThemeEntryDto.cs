using System.Text.Json.Serialization;

namespace applanch.Theming;

public sealed record ThemeEntryDto(
    [property: JsonPropertyName("key")]
    string Key,
    [property: JsonPropertyName("hex")]
    [property: JsonConverter(typeof(ThemeColorJsonConverter))] ThemeColor Hex);
