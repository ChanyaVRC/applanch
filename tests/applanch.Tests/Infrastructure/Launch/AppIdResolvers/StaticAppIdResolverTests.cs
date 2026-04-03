using applanch.Infrastructure.Launch.AppIdResolvers;
using applanch.Infrastructure.Utilities;
using Xunit;

namespace applanch.Tests.Infrastructure.Launch.AppIdResolvers;

public class StaticAppIdResolverTests
{
    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void TryResolve_BlankAppId_ReturnsFalse(string appId)
    {
        var resolver = new StaticAppIdResolver(appId);

        var result = resolver.TryResolve(new LaunchPath(@"C:\Games\game.exe"), out var resolved);

        Assert.False(result);
        Assert.Equal(string.Empty, resolved);
    }

    [Fact]
    public void TryResolve_ValidAppId_ReturnsTrueAndValue()
    {
        var resolver = new StaticAppIdResolver("12345");

        var result = resolver.TryResolve(new LaunchPath(@"C:\Games\game.exe"), out var resolved);

        Assert.True(result);
        Assert.Equal("12345", resolved);
    }

    [Fact]
    public void TryResolve_LaunchPathIsIgnored()
    {
        var resolver = new StaticAppIdResolver("abc");

        resolver.TryResolve(new LaunchPath(@"C:\Games\ignored.exe"), out var resolved);

        Assert.Equal("abc", resolved);
    }
}
