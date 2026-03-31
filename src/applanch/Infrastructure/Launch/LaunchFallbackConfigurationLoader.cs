using System.IO;
using System.Text.Json;
using applanch.Infrastructure.Utilities;

namespace applanch.Infrastructure.Launch;

internal static class LaunchFallbackConfigurationLoader
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
    };

    internal static LaunchFallbackConfiguration Load()
    {
        foreach (var path in GetCandidatePaths())
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
                    return config;
                }
            }
            catch (Exception ex)
            {
                AppLogger.Instance.Warn($"Failed to load launch fallback config '{path}': {ex.Message}");
            }
        }

        AppLogger.Instance.Warn("Launch fallback config not found. Using built-in defaults.");
        return CreateBuiltInDefaults();
    }

    private static IEnumerable<string> GetCandidatePaths()
    {
        var localOverride = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "applanch",
            "launch-fallbacks.json");

        var bundled = Path.Combine(AppContext.BaseDirectory, "Config", "launch-fallbacks.json");

        yield return localOverride;
        yield return bundled;
    }

    private static LaunchFallbackConfiguration CreateBuiltInDefaults()
    {
        return new LaunchFallbackConfiguration
        {
            Rules =
            [
                new LaunchFallbackRuleConfiguration
                {
                    Name = "Riot VALORANT",
                    Kind = "riot-client",
                    MatchFileNames = ["VALORANT.exe"],
                    Product = "valorant",
                    Patchline = "live",
                },
                new LaunchFallbackRuleConfiguration
                {
                    Name = "Riot League of Legends",
                    Kind = "riot-client",
                    MatchFileNames = ["LeagueClient.exe"],
                    Product = "league_of_legends",
                    Patchline = "live",
                },
                new LaunchFallbackRuleConfiguration
                {
                    Name = "Steam library executable",
                    Kind = "steam-rungameid",
                    PathContains = "steamapps/common/",
                },
            ],
        };
    }
}
