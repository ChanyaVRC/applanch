using System.Text.Json;
using System.Text.Json.Serialization;

namespace applanch.Infrastructure.Storage;

internal sealed class CategoryJsonConverter : JsonConverter<Category>
{
    public override Category Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.String)
        {
            throw new JsonException("Category must be a JSON string.");
        }

        return Category.FromInput(reader.GetString());
    }

    public override void Write(Utf8JsonWriter writer, Category value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(value.Value);
    }
}
