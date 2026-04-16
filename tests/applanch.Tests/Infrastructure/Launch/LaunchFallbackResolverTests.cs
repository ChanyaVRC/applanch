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

        var result = resolver.TryCreate(new LaunchPath(@"C:\Games\Game.exe"), runAsAdministrator: false);

        var fallback = Assert.IsType<LaunchFallbackResult>(result);
        Assert.Equal("Epic sample", fallback.Name);
        Assert.Equal("com.epicgames.launcher://apps/ExampleGame?action=launch&silent=true", fallback.StartInfo.FileName);
        Assert.True(fallback.StartInfo.UseShellExecute);
        Assert.Equal(string.Empty, fallback.StartInfo.Arguments);
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

        var result = resolver.TryCreate(new LaunchPath(@"C:\Games\Game.exe"), runAsAdministrator: true);

        var fallback = Assert.IsType<LaunchFallbackResult>(result);
        Assert.Equal("runas", fallback.StartInfo.Verb);
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

            var result = resolver.TryCreate(new LaunchPath(@"C:\Games\Space Game\Game.exe"), runAsAdministrator: false);

            var fallback = Assert.IsType<LaunchFallbackResult>(result);
            Assert.Equal("Generic launcher", fallback.Name);
            Assert.Equal(launcherPath, fallback.StartInfo.FileName);
            Assert.Equal("launch --id game-123 --path \"C:\\Games\\Space Game\\Game.exe\" --dir \"C:\\Games\\Space Game\"", fallback.StartInfo.Arguments);
        }
        finally
        {
            Environment.SetEnvironmentVariable("APPLANCH_TEST_LAUNCHER", originalTemp);
        }
    }

    [Fact]
    public void TryCreate_CommandTemplateRule_ExpandsAncestorPathTokens()
    {
        using var tempDirectory = TemporaryDirectory.Create();
        var riotRoot = Path.Combine(tempDirectory.Path, "Riot Games");
        var gamePath = Path.Combine(riotRoot, "VALORANT", "live", "VALORANT.exe");
        var riotClientPath = Path.Combine(riotRoot, "Riot Client", "RiotClientServices.exe");

        Directory.CreateDirectory(Path.GetDirectoryName(gamePath)!);
        Directory.CreateDirectory(Path.GetDirectoryName(riotClientPath)!);
        File.WriteAllText(gamePath, string.Empty);
        File.WriteAllText(riotClientPath, string.Empty);

        var configuration = new LaunchFallbackConfiguration
        {
            Rules =
            [
                new LaunchFallbackRuleConfiguration
                {
                    Name = "Riot generic",
                    Kind = "command-template",
                    MatchFileNames = ["VALORANT.exe"],
                    FileNameTemplate = "{ancestorPath:Riot Games}\\Riot Client\\RiotClientServices.exe",
                    ArgumentsTemplate = "--launch-product={product} --launch-patchline={patchline}",
                    Product = "valorant",
                    Patchline = "live",
                },
            ],
        };

        var resolver = new LaunchFallbackResolver(configuration);

        var result = resolver.TryCreate(new LaunchPath(gamePath), runAsAdministrator: false);

        var fallback = Assert.IsType<LaunchFallbackResult>(result);
        Assert.Equal("Riot generic", fallback.Name);
        Assert.Equal(riotClientPath, fallback.StartInfo.FileName);
        Assert.Equal("--launch-product=valorant --launch-patchline=live", fallback.StartInfo.Arguments);
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

        var result = resolver.TryCreate(new LaunchPath(gamePath), runAsAdministrator: false);

        var fallback = Assert.IsType<LaunchFallbackResult>(result);
        Assert.Equal("Steam generic", fallback.Name);
        Assert.Equal("steam://rungameid/12345", fallback.StartInfo.FileName);
        Assert.Equal(string.Empty, fallback.StartInfo.Arguments);
    }
}
