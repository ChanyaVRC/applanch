using System.Diagnostics;
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
            Debug.Assert(cultureCode is not null, "Property names must be strings, and GetString() should not return null here.");

            if (!reader.Read())
            {
                throw new JsonException("LocalizedText contains an incomplete property.");
            }

            if (reader.TokenType != JsonTokenType.String)
            {
                throw new JsonException("LocalizedText values must be strings.");
            }

            var text = reader.GetString();
            Debug.Assert(text is not null, "LocalizedText values must be strings, and GetString() should not return null here.");

            if (LanguageOption.TryMapFromCultureCode(cultureCode, out var language))
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
        foreach (var (language, text) in value.Translations)
        {
            writer.WriteString(language.Code, text);
        }

        writer.WriteEndObject();
    }
}
