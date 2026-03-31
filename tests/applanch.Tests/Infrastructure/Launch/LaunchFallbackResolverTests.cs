using applanch.Infrastructure.Launch;
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
}