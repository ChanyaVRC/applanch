using applanch.Infrastructure.Launch.AppIdResolvers;
using Xunit;

namespace applanch.Tests.Infrastructure.Launch.AppIdResolvers;

public class AppIdResolverFactoryTests
{
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void CreateResolver_NullOrWhitespace_ReturnsNull(string? source)
    {
        Assert.Null(AppIdResolverFactory.CreateResolver(source));
    }

    [Fact]
    public void CreateResolver_StaticPrefix_ReturnsStaticResolver()
    {
        var resolver = AppIdResolverFactory.CreateResolver("static:12345");

        Assert.IsType<StaticAppIdResolver>(resolver);
    }

    [Fact]
    public void CreateResolver_StaticPrefix_CaseInsensitive()
    {
        var resolver = AppIdResolverFactory.CreateResolver("STATIC:value");

        Assert.IsType<StaticAppIdResolver>(resolver);
    }

    [Fact]
    public void CreateResolver_SteamManifest_ReturnsSteamResolver()
    {
        var resolver = AppIdResolverFactory.CreateResolver("steam-manifest");

        Assert.IsType<SteamManifestAppIdResolver>(resolver);
    }

    [Fact]
    public void CreateResolver_SteamManifest_CaseInsensitive()
    {
        var resolver = AppIdResolverFactory.CreateResolver("STEAM-MANIFEST");

        Assert.IsType<SteamManifestAppIdResolver>(resolver);
    }

    [Fact]
    public void CreateResolver_RegistryPrefix_ReturnsRegistryResolver()
    {
        var resolver = AppIdResolverFactory.CreateResolver("registry:HKEY_LOCAL_MACHINE:SOFTWARE:Value");

        Assert.IsType<RegistryAppIdResolver>(resolver);
    }

    [Fact]
    public void CreateResolver_RegistryPrefix_CaseInsensitive()
    {
        var resolver = AppIdResolverFactory.CreateResolver("REGISTRY:HKEY_LOCAL_MACHINE:SOFTWARE:Value");

        Assert.IsType<RegistryAppIdResolver>(resolver);
    }

    [Fact]
    public void CreateResolver_UnknownPrefix_ReturnsNull()
    {
        Assert.Null(AppIdResolverFactory.CreateResolver("gog-manifest"));
    }

    [Fact]
    public void CreateResolver_LeadingWhitespace_IsIgnored()
    {
        var resolver = AppIdResolverFactory.CreateResolver("  static:abc  ");

        Assert.IsType<StaticAppIdResolver>(resolver);
    }
}
