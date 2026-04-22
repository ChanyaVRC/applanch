using System.Text.Json;
using applanch.Serialization;

namespace applanch.Theming;

public static class ThemePaletteConfigurationJsonSerializer
{
    private static readonly JsonSerializerOptions ReadOptions = new(JsonConfigLoader.DefaultSerializerOptions)
    {
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true,
    };

    public static ThemePaletteConfigurationDto Deserialize(string json)
    {
        ArgumentNullException.ThrowIfNull(json);
        return JsonConfigLoader.Deserialize<ThemePaletteConfigurationDto>(json, ReadOptions);
    }

    public static ThemePaletteConfigurationDto DeserializeFile(string path)
    {
        ArgumentNullException.ThrowIfNull(path);
        return JsonConfigLoader.DeserializeFile<ThemePaletteConfigurationDto>(path, ReadOptions);
    }

    public static string Serialize(ThemePaletteConfigurationDto document, bool writeIndented = true)
    {
        ArgumentNullException.ThrowIfNull(document);

        var options = new JsonSerializerOptions(ReadOptions)
        {
            WriteIndented = writeIndented,
        };

        return JsonConfigLoader.Serialize(document, options);
    }

    public static void SerializeFile(string path, ThemePaletteConfigurationDto document, bool writeIndented = true)
    {
        ArgumentNullException.ThrowIfNull(path);
        ArgumentNullException.ThrowIfNull(document);

        var options = new JsonSerializerOptions(ReadOptions)
        {
            WriteIndented = writeIndented,
        };

        JsonConfigLoader.SerializeFile(path, document, options);
    }
}
