using applanch.Infrastructure.Launch.AppIdResolvers;
using applanch.Infrastructure.Utilities;
using Xunit;

namespace applanch.Tests.Infrastructure.Launch.AppIdResolvers;

public class RegistryAppIdResolverTests
{
    [Theory]
    [InlineData("registry:HKEY_LOCAL_MACHINE:SOFTWARE")]           // 3 parts — missing ValueName
    [InlineData("registry:HKEY_LOCAL_MACHINE")]                    // 2 parts
    [InlineData("not-registry:HKEY_LOCAL_MACHINE:SOFTWARE:Value")] // wrong prefix
    public void CanResolve_MalformedSource_ReturnsFalse(string source)
    {
        var resolver = new RegistryAppIdResolver(source);

        var result = resolver.CanResolve(new LaunchPath(@"C:\game.exe"));

        Assert.False(result);
    }

    [Fact]
    public void CanResolve_UnknownHiveName_ReturnsFalse()
    {
        var resolver = new RegistryAppIdResolver("registry:HKEY_BOGUS:SOFTWARE:Value");

        var result = resolver.CanResolve(new LaunchPath(@"C:\game.exe"));

        Assert.False(result);
    }

    [Theory]
    [InlineData("registry:HKEY_LOCAL_MACHINE:SOFTWARE\\applanch_test_nonexistent:Value")]
    [InlineData("registry:HKEY_CURRENT_USER:SOFTWARE\\applanch_test_nonexistent:Value")]
    [InlineData("registry:HKEY_CLASSES_ROOT:applanch_test_nonexistent:Value")]
    [InlineData("registry:HKEY_USERS:applanch_test_nonexistent:Value")]
    [InlineData("registry:HKEY_CURRENT_CONFIG:SOFTWARE\\applanch_test_nonexistent:Value")]
    public void Resolve_ValidHiveButNonExistentKey_Throws(string source)
    {
        var resolver = new RegistryAppIdResolver(source);

        var ex = Assert.Throws<AppIdResolutionException>(() => resolver.Resolve(new LaunchPath(@"C:\game.exe")));

        Assert.Contains("registry app-id source", ex.Message, StringComparison.OrdinalIgnoreCase);
    }
}
