using applanch.Infrastructure.Integration;
using applanch.Serialization;
using applanch.Tests.TestSupport;
using Xunit;

namespace applanch.Tests.Infrastructure.Integration;

public sealed class LaunchItemIconPathResolverTests
{
    [Fact]
    public void Resolve_DiscordUpdatePath_MapsToNewestDiscordExe()
    {
        using var temp = TemporaryDirectory.Create("applanch-icon-map-discord");
        var discordRoot = Path.Combine(temp.Path, "Discord");
        var appOld = Path.Combine(discordRoot, "app-1.0.0");
        var appNew = Path.Combine(discordRoot, "app-1.1.0");
        Directory.CreateDirectory(appOld);
        Directory.CreateDirectory(appNew);

        var oldExe = Path.Combine(appOld, "Discord.exe");
        var newExe = Path.Combine(appNew, "Discord.exe");
        File.WriteAllBytes(oldExe, [0x01]);
        File.WriteAllBytes(newExe, [0x02]);
        File.SetLastWriteTimeUtc(oldExe, DateTime.UtcNow.AddHours(-1));
        File.SetLastWriteTimeUtc(newExe, DateTime.UtcNow);

        var updateExe = Path.Combine(discordRoot, "Update.exe");
        File.WriteAllBytes(updateExe, [0x03]);

        var resolver = new LaunchItemIconPathResolver(new LaunchItemIconPathMappingConfiguration
        {
            Rules =
            [
                new LaunchItemIconPathMappingRuleConfiguration
                {
                    Name = "Discord",
                    Enabled = true,
                    MatchFileNames = ["Update.exe"],
                    ParentDirectoryName = "Discord",
                    IconPathTemplate = "app-*\\Discord.exe",
                },
            ],
        });

        var resolved = resolver.Resolve(updateExe);

        Assert.Equal(newExe, resolved, ignoreCase: true);
    }

    [Fact]
    public void Resolve_NoMatchingRule_ReturnsOriginalPath()
    {
        var input = @"C:\Tools\SampleApp\Sample.exe";
        var resolver = new LaunchItemIconPathResolver(new LaunchItemIconPathMappingConfiguration
        {
            Rules =
            [
                new LaunchItemIconPathMappingRuleConfiguration
                {
                    Name = "Discord",
                    Enabled = true,
                    MatchFileNames = ["Update.exe"],
                    ParentDirectoryName = "Discord",
                    IconPathTemplate = "app-*\\Discord.exe",
                },
            ],
        });

        var resolved = resolver.Resolve(input);

        Assert.Equal(input, resolved, ignoreCase: true);
    }

    [Fact]
    public void Resolve_UpdateExeNotDirectlyUnderDiscord_ReturnsOriginalPath()
    {
        var input = @"C:\Program Files\Discord\bin\Update.exe";
        var resolver = new LaunchItemIconPathResolver(new LaunchItemIconPathMappingConfiguration
        {
            Rules =
            [
                new LaunchItemIconPathMappingRuleConfiguration
                {
                    Name = "Discord",
                    Enabled = true,
                    MatchFileNames = ["Update.exe"],
                    ParentDirectoryName = "Discord",
                    IconPathTemplate = "app.ico",
                },
            ],
        });

        var resolved = resolver.Resolve(input);

        Assert.Equal(input, resolved, ignoreCase: true);
    }

    [Fact]
    public void Resolve_MatchingRuleWithInvalidTemplate_ThrowsConfigurationError()
    {
        var input = @"C:\Program Files\Discord\Update.exe";
        var resolver = new LaunchItemIconPathResolver(new LaunchItemIconPathMappingConfiguration
        {
            Rules =
            [
                new LaunchItemIconPathMappingRuleConfiguration
                {
                    Name = "Discord",
                    Enabled = true,
                    MatchFileNames = ["Update.exe"],
                    ParentDirectoryName = "Discord",
                    IconPathTemplate = "missing-icon.ico",
                },
            ],
        });

        var exception = Assert.Throws<JsonPathResolutionException>(() => resolver.Resolve(input));
        Assert.NotNull(exception.InnerException);
    }
}
