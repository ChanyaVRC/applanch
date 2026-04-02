using System.Text.Json;
using applanch.Infrastructure.Launch;
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
            rule.GetProperty("kind").GetString() == "command-template" &&
            rule.GetProperty("fallbackTrigger").GetString() == "always" &&
            rule.GetProperty("fileNameTemplate").GetString()!.Contains("{ancestorPath:Riot Games}", StringComparison.Ordinal) &&
            rule.GetProperty("matchFileNames").EnumerateArray().Any(value => value.GetString() == "VALORANT-Win64-Shipping.exe"));

        Assert.Contains(rules.EnumerateArray(), rule =>
            rule.GetProperty("name").GetString() == "Riot League of Legends" &&
            rule.GetProperty("kind").GetString() == "command-template" &&
            rule.GetProperty("fallbackTrigger").GetString() == "always" &&
            rule.GetProperty("matchFileNames").EnumerateArray().Any(value => value.GetString() == "LeagueClientUx.exe") &&
            rule.GetProperty("matchFileNames").EnumerateArray().Any(value => value.GetString() == "League of Legends.exe"));

        Assert.Contains(rules.EnumerateArray(), rule =>
            rule.GetProperty("name").GetString() == "Steam library executable" &&
            rule.GetProperty("kind").GetString() == "uri-template" &&
            rule.GetProperty("fallbackTrigger").GetString() == "always" &&
            rule.GetProperty("appIdSource").GetString() == "steam-manifest");

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

        Assert.DoesNotContain(rules.EnumerateArray(), rule =>
            rule.GetProperty("name").GetString() == "Generic launcher executable sample");

        Assert.DoesNotContain(rules.EnumerateArray(), rule =>
            rule.GetProperty("name").GetString() == "Custom URI sample");
    }

    [Fact]
    public void UserDefinedSampleConfig_InRepository_IsLoadableByCurrentLoader()
    {
        var root = CreateTempDirectory();
        var appBase = Path.Combine(root, "appbase");
        var userDefinedDirectory = Path.Combine(appBase, "Config", "UserDefined", "launch-fallbacks");
        Directory.CreateDirectory(userDefinedDirectory);

        var projectRoot = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", ".."));
        var samplePath = Path.Combine(projectRoot, "src", "applanch", "Config", "UserDefined", "launch-fallbacks", "launch-fallbacks.sample.json");
        File.Copy(samplePath, Path.Combine(userDefinedDirectory, "launch-fallbacks.sample.json"));

        try
        {
            var configuration = LaunchFallbackConfigurationLoader.LoadFromDirectory(appBase);

            Assert.Equal(2, configuration.Rules.Count);
            Assert.All(configuration.Rules, static rule => Assert.False(rule.Enabled));
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    private static string CreateTempDirectory()
    {
        var path = Path.Combine(Path.GetTempPath(), "applanch-launch-config-tests-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(path);
        return path;
    }
}
