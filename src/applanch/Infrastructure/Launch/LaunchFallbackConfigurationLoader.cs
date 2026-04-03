using System.IO;
using System.Text.Json;
using applanch.Infrastructure.Utilities;

namespace applanch.Infrastructure.Launch;

internal static class LaunchFallbackConfigurationLoader
{
    private const string UserDefinedDirectoryName = "UserDefined";
    private const string UserDefinedLaunchFallbacksDirectoryName = "launch-fallbacks";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
    };

    internal static LaunchFallbackConfiguration Load()
    {
        return LoadFromDirectory(AppContext.BaseDirectory);
    }

    internal static LaunchFallbackConfiguration LoadFromDirectory(string appBaseDirectory)
    {
        var configDirectory = Path.Combine(appBaseDirectory, "Config");
        var userDefinedDirectory = Path.Combine(configDirectory, UserDefinedDirectoryName, UserDefinedLaunchFallbacksDirectoryName);

        var merged = new LaunchFallbackConfiguration { Rules = [] };
        var loadedAny = false;

        foreach (var path in GetCandidatePaths(configDirectory, userDefinedDirectory))
        {
            if (!File.Exists(path))
            {
                continue;
            }

            try
            {
                var json = File.ReadAllText(path);
                var config = JsonSerializer.Deserialize<LaunchFallbackConfiguration>(json, JsonOptions);
                if (config is not null)
                {
                    AppLogger.Instance.Info($"Loaded launch fallback config: {path}");
                    merged.Rules.AddRange(config.Rules);
                    loadedAny = true;
                }
            }
            catch (Exception ex)
            {
                AppLogger.Instance.Warn($"Failed to load launch fallback config '{path}': {ex.Message}");
            }
        }

        if (!loadedAny)
        {
            AppLogger.Instance.Info("Launch fallback config not found.");
        }

        return merged;
    }

    private static IEnumerable<string> GetCandidatePaths(string configDirectory, string userDefinedDirectory)
    {
        var bundled = Path.Combine(configDirectory, "launch-fallbacks.json");
        yield return bundled;

        if (!Directory.Exists(userDefinedDirectory))
        {
            yield break;
        }

        foreach (var userDefined in Directory
                     .EnumerateFiles(userDefinedDirectory, "*.json", SearchOption.TopDirectoryOnly)
                     .OrderBy(static path => path, StringComparer.OrdinalIgnoreCase))
        {
            yield return userDefined;
        }
    }
}
