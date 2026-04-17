using System.Text.Json;
using System.Text.Json.Serialization;

namespace applanch.Localization;

internal sealed class LanguageOptionJsonConverter : JsonConverter<LanguageOption>
{
    public override LanguageOption Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Number)
        {
            if (!reader.TryGetInt32(out var numericValue))
            {
                throw new JsonException("Language numeric value must be a 32-bit integer.");
            }

            return numericValue switch
            {
                0 => LanguageOption.System,
                1 => LanguageOption.English,
                2 => LanguageOption.Japanese,
                _ => throw new JsonException($"Unsupported language numeric value '{numericValue}'."),
            };
        }

        if (reader.TokenType != JsonTokenType.String)
        {
            throw new JsonException("Language must be a string language code or a legacy numeric value.");
        }

        var raw = reader.GetString()!;
        if (LanguageOption.TryFromCode(raw, out var mapped))
        {
            return mapped;
        }

        throw new JsonException($"Unsupported language code '{raw}'.");
    }

    public override void Write(Utf8JsonWriter writer, LanguageOption value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(value.Code);
    }
}
