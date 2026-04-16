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
    /// Deserializes a JSON string.
    /// </summary>
    /// <typeparam name="T">The target type to deserialize to.</typeparam>
    /// <param name="json">JSON string content.</param>
    /// <returns>The deserialized object.</returns>
    /// <exception cref="InvalidOperationException">Thrown when deserialization fails.</exception>
    public static T Deserialize<T>(string json)
    {
        return System.Text.Json.JsonSerializer.Deserialize<T>(
            json,
            DefaultSerializerOptions)
            ?? throw new InvalidOperationException(
                $"Failed to deserialize JSON to type '{typeof(T).Name}'.");
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
