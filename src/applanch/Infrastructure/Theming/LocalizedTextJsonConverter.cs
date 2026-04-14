using System.Text.Json;
using System.Text.Json.Serialization;
using applanch.Infrastructure.Storage;

namespace applanch.Infrastructure.Theming;

internal sealed class LocalizedTextJsonConverter : JsonConverter<LocalizedText>
{
    public override LocalizedText Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Null)
        {
            return new LocalizedText(string.Empty);
        }

        if (reader.TokenType != JsonTokenType.StartObject)
        {
            throw new JsonException("LocalizedText must be a JSON object.");
        }

        var translations = new Dictionary<LanguageOption, string>();
        string? fallback = null;

        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.EndObject)
            {
                break;
            }

            if (reader.TokenType != JsonTokenType.PropertyName)
            {
                throw new JsonException("LocalizedText contains an invalid token.");
            }

            var cultureCode = reader.GetString();
            if (!reader.Read())
            {
                throw new JsonException("LocalizedText contains an incomplete property.");
            }

            if (reader.TokenType != JsonTokenType.String)
            {
                throw new JsonException("LocalizedText values must be strings.");
            }

            var text = reader.GetString();
            if (string.IsNullOrWhiteSpace(text))
            {
                continue;
            }

            if (LanguageOption.TryMapFromCultureCode(cultureCode ?? string.Empty, out var language))
            {
                translations[language] = text;
                if (language == LanguageOption.PrimaryFallbackLanguage)
                {
                    fallback = text;
                }
            }
        }

        fallback ??= translations.Count > 0
            ? translations.Values.First()
            : string.Empty;

        return new LocalizedText(fallback, translations.Count > 0 ? translations : null);
    }

    public override void Write(Utf8JsonWriter writer, LocalizedText value, JsonSerializerOptions options)
    {
        writer.WriteStartObject();
        writer.WriteString(LanguageOption.English.Code, value.Resolve(LanguageOption.English));
        writer.WriteString(LanguageOption.Japanese.Code, value.Resolve(LanguageOption.Japanese));
        writer.WriteEndObject();
    }
}
