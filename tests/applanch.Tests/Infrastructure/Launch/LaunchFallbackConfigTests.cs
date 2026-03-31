using System.Text.Json;
using Xunit;

namespace applanch.Tests.Infrastructure.Launch;

public class LaunchFallbackConfigTests
{
    [Fact]
    public void BundledConfig_IncludesExpandedHighConfidenceLauncherRules()
    {
        var projectRoot = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", ".."));
        var configPath = Path.Combine(projectRoot, "src", "applanch", "Config", "launch-fallbacks.json");
        var json = File.ReadAllText(configPath);

        using var document = JsonDocument.Parse(json, new JsonDocumentOptions
        {
            CommentHandling = JsonCommentHandling.Skip,
        });

        var rules = document.RootElement.GetProperty("rules");

        Assert.Contains(rules.EnumerateArray(), rule =>
            rule.GetProperty("name").GetString() == "Riot VALORANT" &&
            rule.GetProperty("matchFileNames").EnumerateArray().Any(value => value.GetString() == "VALORANT-Win64-Shipping.exe"));

        Assert.Contains(rules.EnumerateArray(), rule =>
            rule.GetProperty("name").GetString() == "Riot League of Legends" &&
            rule.GetProperty("matchFileNames").EnumerateArray().Any(value => value.GetString() == "LeagueClientUx.exe") &&
            rule.GetProperty("matchFileNames").EnumerateArray().Any(value => value.GetString() == "League of Legends.exe"));

        Assert.Contains(rules.EnumerateArray(), rule =>
            rule.GetProperty("name").GetString() == "Epic Games sample" &&
            rule.GetProperty("kind").GetString() == "uri-template");

        Assert.Contains(rules.EnumerateArray(), rule =>
            rule.GetProperty("name").GetString() == "Ubisoft Connect sample" &&
            rule.GetProperty("kind").GetString() == "uri-template");

        Assert.Contains(rules.EnumerateArray(), rule =>
            rule.GetProperty("name").GetString() == "EA app sample" &&
            rule.GetProperty("kind").GetString() == "uri-template");

        Assert.Contains(rules.EnumerateArray(), rule =>
            rule.GetProperty("name").GetString() == "Battle.net sample" &&
            rule.GetProperty("kind").GetString() == "uri-template");

        Assert.Contains(rules.EnumerateArray(), rule =>
            rule.GetProperty("name").GetString() == "Generic launcher executable sample" &&
            rule.GetProperty("kind").GetString() == "command-template");

        Assert.Contains(rules.EnumerateArray(), rule =>
            rule.GetProperty("name").GetString() == "Custom URI sample" &&
            rule.GetProperty("uriTemplate").GetString()!.Contains("{launchFileName}", StringComparison.Ordinal));
    }
}
