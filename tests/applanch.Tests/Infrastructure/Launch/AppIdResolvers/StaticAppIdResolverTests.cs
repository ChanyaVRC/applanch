using applanch.Infrastructure.Launch.AppIdResolvers;
using Xunit;

namespace applanch.Tests.Infrastructure.Launch.AppIdResolvers;

public class StaticAppIdResolverTests
{
    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void CanResolve_BlankAppId_ReturnsFalse(string appId)
    {
        var resolver = new StaticAppIdResolver(appId);

        var result = resolver.CanResolve(new LaunchPath(@"C:\Games\game.exe"));

        Assert.False(result);
    }

    [Fact]
    public void CanResolve_ValidAppId_ReturnsTrue()
    {
        var resolver = new StaticAppIdResolver("12345");

        var result = resolver.CanResolve(new LaunchPath(@"C:\Games\game.exe"));

        Assert.True(result);
    }

    [Fact]
    public void Resolve_ValidAppId_ReturnsValue()
    {
        var resolver = new StaticAppIdResolver("abc");

        var resolved = resolver.Resolve(new LaunchPath(@"C:\Games\ignored.exe"));

        Assert.Equal("abc", resolved);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Resolve_BlankAppId_Throws(string appId)
    {
        var resolver = new StaticAppIdResolver(appId);

        var ex = Assert.Throws<AppIdResolutionException>(() => resolver.Resolve(new LaunchPath(@"C:\Games\game.exe")));

        Assert.Equal("Static app ID is empty.", ex.Message);
    }
}
