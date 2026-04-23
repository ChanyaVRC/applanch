using System.Text.Json;
using System.Text.Json.Serialization;

namespace applanch.Theming;

public sealed class ThemeColorJsonConverter : JsonConverter<ThemeColor>
{
    public override ThemeColor Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.String)
        {
            throw new JsonException("Theme color must be a string.");
        }

        var value = reader.GetString();
        if (ThemeColor.TryParse(value, out var color))
        {
            return color;
        }

        throw new JsonException($"Invalid theme color: '{value}'.");
    }

    public override void Write(Utf8JsonWriter writer, ThemeColor value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(value.Hex);
    }
}
