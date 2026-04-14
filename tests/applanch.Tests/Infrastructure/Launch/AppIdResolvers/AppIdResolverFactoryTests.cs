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

    [Theory]
    [InlineData("static:12345", typeof(StaticAppIdResolver))]
    [InlineData("steam-manifest", typeof(SteamManifestAppIdResolver))]
    [InlineData("registry:HKEY_LOCAL_MACHINE:SOFTWARE:Value", typeof(RegistryAppIdResolver))]
    [InlineData("  static:abc  ", typeof(StaticAppIdResolver))]
    public void CreateResolver_SupportedSource_ReturnsExpectedResolver(string source, Type expectedType)
    {
        var resolver = AppIdResolverFactory.CreateResolver(source);

        Assert.IsType(expectedType, resolver);
    }

    [Theory]
    [InlineData("STATIC:value")]
    [InlineData("STEAM-MANIFEST")]
    [InlineData("REGISTRY:HKEY_LOCAL_MACHINE:SOFTWARE:Value")]
    [InlineData("gog-manifest")]
    public void CreateResolver_UnsupportedOrNonCanonicalSource_ReturnsNull(string source)
    {
        var resolver = AppIdResolverFactory.CreateResolver(source);

        Assert.Null(resolver);
    }
}
