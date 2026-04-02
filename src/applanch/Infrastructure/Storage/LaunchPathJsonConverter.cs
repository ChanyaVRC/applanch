using System.Text.Json;
using System.Text.Json.Serialization;
using applanch.Infrastructure.Utilities;

namespace applanch.Infrastructure.Storage;

internal sealed class LaunchPathJsonConverter : JsonConverter<LaunchPath>
{
    public override LaunchPath Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.String)
        {
            throw new JsonException("Launch path must be a JSON string.");
        }

        var value = reader.GetString();
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new JsonException("Launch path string must not be empty.");
        }

        return new LaunchPath(value);
    }

    public override void Write(Utf8JsonWriter writer, LaunchPath value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(value.Value);
    }
}
