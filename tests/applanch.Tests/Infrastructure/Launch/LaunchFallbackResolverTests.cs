using applanch.Infrastructure.Launch;
using applanch.Tests.TestSupport;
using Xunit;

namespace applanch.Tests.Infrastructure.Launch;

public class LaunchFallbackResolverTests
{
    [Fact]
    public void TryCreate_UriTemplateRule_ExpandsAppIdIntoShellTarget()
    {
        var configuration = new LaunchFallbackConfiguration
        {
            Rules =
            [
                new LaunchFallbackRuleConfiguration
                {
                    Name = "Epic sample",
                    Kind = "uri-template",
                    MatchFileNames = ["Game.exe"],
                    UriTemplate = "com.epicgames.launcher://apps/{appId}?action=launch&silent=true",
                    AppId = "ExampleGame",
                },
            ],
        };

        var resolver = new LaunchFallbackResolver(configuration);

        var matched = resolver.TryCreate(@"C:\Games\Game.exe", runAsAdministrator: false, out var fallback, out var fallbackName);

        Assert.True(matched);
        Assert.Equal("Epic sample", fallbackName);
        Assert.Equal("com.epicgames.launcher://apps/ExampleGame?action=launch&silent=true", fallback.FileName);
        Assert.True(fallback.UseShellExecute);
        Assert.Equal(string.Empty, fallback.Arguments);
    }

    [Fact]
    public void TryCreate_UriTemplateRule_WithRunAsAdministrator_PreservesRunAsVerb()
    {
        var configuration = new LaunchFallbackConfiguration
        {
            Rules =
            [
                new LaunchFallbackRuleConfiguration
                {
                    Name = "Ubisoft sample",
                    Kind = "uri-template",
                    MatchFileNames = ["Game.exe"],
                    UriTemplate = "uplay://launch/{appId}/0",
                    AppId = "1234",
                },
            ],
        };

        var resolver = new LaunchFallbackResolver(configuration);

        var matched = resolver.TryCreate(@"C:\Games\Game.exe", runAsAdministrator: true, out var fallback, out _);

        Assert.True(matched);
        Assert.Equal("runas", fallback.Verb);
    }

    [Fact]
    public void TryCreate_CommandTemplateRule_ExpandsTokensAndEnvironmentVariables()
    {
        using var tempDirectory = TemporaryDirectory.Create();
        var launcherPath = Path.Combine(tempDirectory.Path, "launcher.exe");
        File.WriteAllText(launcherPath, string.Empty);

        var originalTemp = Environment.GetEnvironmentVariable("APPLANCH_TEST_LAUNCHER");
        Environment.SetEnvironmentVariable("APPLANCH_TEST_LAUNCHER", launcherPath);
        try
        {
            var configuration = new LaunchFallbackConfiguration
            {
                Rules =
                [
                    new LaunchFallbackRuleConfiguration
                    {
                        Name = "Generic launcher",
                        Kind = "command-template",
                        MatchFileNames = ["Game.exe"],
                        FileNameTemplate = "%APPLANCH_TEST_LAUNCHER%",
                        ArgumentsTemplate = "launch --id {appId} --path {launchPathQuoted} --dir {launchDirectoryQuoted}",
                        AppId = "game-123",
                    },
                ],
            };

            var resolver = new LaunchFallbackResolver(configuration);

            var matched = resolver.TryCreate(@"C:\Games\Space Game\Game.exe", runAsAdministrator: false, out var fallback, out var fallbackName);

            Assert.True(matched);
            Assert.Equal("Generic launcher", fallbackName);
            Assert.Equal(launcherPath, fallback.FileName);
            Assert.Equal("launch --id game-123 --path \"C:\\Games\\Space Game\\Game.exe\" --dir \"C:\\Games\\Space Game\"", fallback.Arguments);
        }
        finally
        {
            Environment.SetEnvironmentVariable("APPLANCH_TEST_LAUNCHER", originalTemp);
        }
    }

    [Fact]
    public void TryCreate_UriTemplateRule_WithSteamManifestAppIdSource_ResolvesAppIdFromManifest()
    {
        using var tempDirectory = TemporaryDirectory.Create();
        var steamApps = Path.Combine(tempDirectory.Path, "Steam", "steamapps");
        var gameDirectory = Path.Combine(steamApps, "common", "CoolGame");
        var gamePath = Path.Combine(gameDirectory, "coolgame.exe");
        var manifest = Path.Combine(steamApps, "appmanifest_12345.acf");

        Directory.CreateDirectory(gameDirectory);
        File.WriteAllText(gamePath, string.Empty);
        File.WriteAllText(manifest,
            "\"AppState\"\n" +
            "{\n" +
            "  \"appid\"  \"12345\"\n" +
            "  \"installdir\"  \"CoolGame\"\n" +
            "}\n");

        var configuration = new LaunchFallbackConfiguration
        {
            Rules =
            [
                new LaunchFallbackRuleConfiguration
                {
                    Name = "Steam generic",
                    Kind = "uri-template",
                    PathContains = "steamapps/common/",
                    UriTemplate = "steam://rungameid/{appId}",
                    AppIdSource = "steam-manifest",
                },
            ],
        };

        var resolver = new LaunchFallbackResolver(configuration);

        var matched = resolver.TryCreate(gamePath, runAsAdministrator: false, out var fallback, out var fallbackName);

        Assert.True(matched);
        Assert.Equal("Steam generic", fallbackName);
        Assert.Equal("steam://rungameid/12345", fallback.FileName);
        Assert.Equal(string.Empty, fallback.Arguments);
    }
}