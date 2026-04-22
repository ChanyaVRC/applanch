namespace applanch.Serialization;

/// <summary>
/// Utility class for type-safe deserialization of JSON files.
/// </summary>
public static class JsonConfigLoader
{
    /// <summary>
    /// Deserializes a JSON file from the specified path.
    /// </summary>
    /// <typeparam name="T">The target type to deserialize to.</typeparam>
    /// <param name="path">Path to the JSON file.</param>
    /// <returns>The deserialized object.</returns>
    /// <exception cref="FileNotFoundException">Thrown when the file cannot be found.</exception>
    /// <exception cref="InvalidOperationException">Thrown when deserialization fails.</exception>
    public static T DeserializeFile<T>(string path)
    {
        if (!File.Exists(path))
        {
            throw new FileNotFoundException("Config file not found.", path);
        }

        var json = File.ReadAllText(path);
        return Deserialize<T>(json);
    }

    /// <summary>
    /// Deserializes a JSON file from the specified path using the provided serializer options.
    /// </summary>
    /// <typeparam name="T">The target type to deserialize to.</typeparam>
    /// <param name="path">Path to the JSON file.</param>
    /// <param name="options">Serializer options to apply.</param>
    /// <returns>The deserialized object.</returns>
    /// <exception cref="FileNotFoundException">Thrown when the file cannot be found.</exception>
    /// <exception cref="InvalidOperationException">Thrown when deserialization fails.</exception>
    public static T DeserializeFile<T>(string path, System.Text.Json.JsonSerializerOptions options)
    {
        if (!File.Exists(path))
        {
            throw new FileNotFoundException("Config file not found.", path);
        }

        var json = File.ReadAllText(path);
        return System.Text.Json.JsonSerializer.Deserialize<T>(json, options)
            ?? throw new InvalidOperationException(
                $"Failed to deserialize JSON to type '{typeof(T).Name}'.");
    }

    /// <summary>
    /// Deserializes a JSON string using the provided serializer options.
    /// </summary>
    /// <typeparam name="T">The target type to deserialize to.</typeparam>
    /// <param name="json">JSON string content.</param>
    /// <param name="options">Serializer options to apply.</param>
    /// <returns>The deserialized object.</returns>
    /// <exception cref="InvalidOperationException">Thrown when deserialization fails.</exception>
    public static T Deserialize<T>(string json, System.Text.Json.JsonSerializerOptions options)
    {
        return System.Text.Json.JsonSerializer.Deserialize<T>(json, options)
            ?? throw new InvalidOperationException(
                $"Failed to deserialize JSON to type '{typeof(T).Name}'.");
    }

    /// <summary>
    /// Deserializes a JSON string.
    /// </summary>
    /// <typeparam name="T">The target type to deserialize to.</typeparam>
    /// <param name="json">JSON string content.</param>
    /// <returns>The deserialized object.</returns>
    /// <exception cref="InvalidOperationException">Thrown when deserialization fails.</exception>
    public static T Deserialize<T>(string json)
    {
        return Deserialize<T>(json, DefaultSerializerOptions);
    }

    /// <summary>
    /// Serializes a value to a JSON string.
    /// </summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="value">The value to serialize.</param>
    /// <returns>The serialized JSON string.</returns>
    public static string Serialize<T>(T value)
        => Serialize(value, DefaultSerializerOptions);

    /// <summary>
    /// Serializes a value to a JSON string using the provided serializer options.
    /// </summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="value">The value to serialize.</param>
    /// <param name="options">Serializer options to apply.</param>
    /// <returns>The serialized JSON string.</returns>
    public static string Serialize<T>(T value, System.Text.Json.JsonSerializerOptions options)
        => System.Text.Json.JsonSerializer.Serialize(value, options);

    /// <summary>
    /// Serializes a value to a JSON file at the specified path.
    /// </summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="path">Path to the JSON file.</param>
    /// <param name="value">The value to serialize.</param>
    public static void SerializeFile<T>(string path, T value)
        => SerializeFile(path, value, DefaultSerializerOptions);

    /// <summary>
    /// Serializes a value to a JSON file at the specified path using the provided serializer options.
    /// </summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="path">Path to the JSON file.</param>
    /// <param name="value">The value to serialize.</param>
    /// <param name="options">Serializer options to apply.</param>
    public static void SerializeFile<T>(string path, T value, System.Text.Json.JsonSerializerOptions options)
    {
        ArgumentNullException.ThrowIfNull(path);

        var directoryPath = Path.GetDirectoryName(path);
        if (!string.IsNullOrWhiteSpace(directoryPath))
        {
            Directory.CreateDirectory(directoryPath);
        }

        File.WriteAllText(path, Serialize(value, options));
    }

    /// <summary>
    /// Parses a JSON file as a document (for scenarios such as counting elements or merging).
    /// </summary>
    /// <param name="path">Path to the JSON file.</param>
    /// <returns>The parsed JSON document.</returns>
    /// <exception cref="FileNotFoundException">Thrown when the file cannot be found.</exception>
    public static System.Text.Json.JsonDocument ParseFile(string path)
    {
        if (!File.Exists(path))
        {
            throw new FileNotFoundException("Config file not found.", path);
        }

        using var stream = File.OpenRead(path);
        return System.Text.Json.JsonDocument.Parse(
            stream,
            DefaultDocumentOptions);
    }

    /// <summary>
    /// Default <see cref="System.Text.Json.JsonSerializerOptions"/>.
    /// Includes support for comments, trailing commas, and case-insensitive property names.
    /// </summary>
    public static System.Text.Json.JsonSerializerOptions DefaultSerializerOptions { get; } = new()
    {
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = System.Text.Json.JsonCommentHandling.Skip,
        AllowTrailingCommas = true,
    };

    /// <summary>
    /// Default <see cref="System.Text.Json.JsonDocumentOptions"/>.
    /// Includes support for comments and trailing commas.
    /// </summary>
    public static System.Text.Json.JsonDocumentOptions DefaultDocumentOptions { get; } = new()
    {
        CommentHandling = System.Text.Json.JsonCommentHandling.Skip,
        AllowTrailingCommas = true,
    };
}
