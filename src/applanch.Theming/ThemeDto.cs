using System.Text.Json.Serialization;
using applanch.Localization;

namespace applanch.Theming;

public sealed record ThemeDto(
    [property: JsonPropertyName("id")]
    string Id,
    [property: JsonPropertyName("displayNames")]
    [property: JsonConverter(typeof(LocalizedTextJsonConverter))] LocalizedText? DisplayNames = null,
    [property: JsonPropertyName("entriesFrom")]
    EntriesFromSpec? EntriesFrom = null,
    [property: JsonPropertyName("entries")]
    IReadOnlyList<ThemeEntryDto>? Entries = null,
    [property: JsonPropertyName("enabled")]
    bool Enabled = true);
