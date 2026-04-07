using System.IO;
using System.Text.Json;
using applanch.Infrastructure.Utilities;

namespace applanch.Infrastructure.Integration;

internal static class LaunchItemIconPathMappingConfigurationLoader
{
    private const string UserDefinedIconPathMappingsDirectoryName = "icon-path-mappings";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true,
    };

    internal static LaunchItemIconPathMappingConfiguration Load()
    {
        return LoadFromDirectory(AppContext.BaseDirectory);
    }

    internal static LaunchItemIconPathMappingConfiguration LoadFromDirectory(string appBaseDirectory)
    {
        var merged = new LaunchItemIconPathMappingConfiguration { Rules = [] };
        var loadedAny = false;

        foreach (var path in ConfigJsonPathResolver.EnumerateBundledAndUserDefined(
                     appBaseDirectory,
                     "icon-path-mappings.json",
                     UserDefinedIconPathMappingsDirectoryName))
        {
            if (!File.Exists(path))
            {
                continue;
            }

            try
            {
                var json = File.ReadAllText(path);
                var config = JsonSerializer.Deserialize<LaunchItemIconPathMappingConfiguration>(json, JsonOptions);
                if (config is not null)
                {
                    AppLogger.Instance.Info($"Loaded icon path mapping config: {path}");
                    merged.Rules.AddRange(config.Rules);
                    loadedAny = true;
                }
            }
            catch (Exception ex)
            {
                AppLogger.Instance.Warn($"Failed to load icon path mapping config '{path}': {ex.Message}");
            }
        }

        if (!loadedAny)
        {
            AppLogger.Instance.Info("Icon path mapping config not found.");
        }

        return merged;
    }
}
