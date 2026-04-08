using System.Text.Json;
using System.Text.Json.Serialization;

namespace applanch.Infrastructure.Theming;

internal sealed class EntriesFromSpecJsonConverter : JsonConverter<EntriesFromSpec>
{
    public override EntriesFromSpec? Read(
        ref Utf8JsonReader reader,
        Type typeToConvert,
        JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Null)
        {
            return null;
        }

        if (reader.TokenType == JsonTokenType.String)
        {
            return new InheritedEntriesFromSpec(reader.GetString() ?? string.Empty);
        }

        if (reader.TokenType == JsonTokenType.StartObject)
        {
            var sources = new Dictionary<SystemThemeMode, string>();
            using var obj = JsonDocument.ParseValue(ref reader);

            foreach (var property in obj.RootElement.EnumerateObject())
            {
                if (property.Value.ValueKind != JsonValueKind.String)
                {
                    continue;
                }

                if (SystemThemeModeExtensions.TryParseFromThemeId(Normalize(property.Name)) is not { } systemMode)
                {
                    continue;
                }

                var source = Normalize(property.Value.GetString());
                if (source.Length > 0)
                {
                    sources[systemMode] = source;
                }
            }

            return new SystemDependentEntriesFromSpec(sources);
        }

        reader.Skip();
        return null;
    }

    public override void Write(Utf8JsonWriter writer, EntriesFromSpec value, JsonSerializerOptions options)
    {
        switch (value)
        {
            case InheritedEntriesFromSpec inherited:
                writer.WriteStringValue(inherited.SourceThemeId);
                break;
            case SystemDependentEntriesFromSpec systemDependent:
                writer.WriteStartObject();
                foreach (var (mode, sourceThemeId) in systemDependent.SourcesByMode)
                {
                    var key = mode.ToThemeId();
                    writer.WriteString(key, sourceThemeId);
                }

                writer.WriteEndObject();
                break;
            default:
                writer.WriteNullValue();
                break;
        }
    }

    private static string Normalize(string? value) =>
        string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim().ToLowerInvariant();
}
