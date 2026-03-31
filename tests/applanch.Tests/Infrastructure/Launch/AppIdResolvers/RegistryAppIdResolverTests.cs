using applanch.Infrastructure.Launch.AppIdResolvers;
using Xunit;

namespace applanch.Tests.Infrastructure.Launch.AppIdResolvers;

public class RegistryAppIdResolverTests
{
    [Theory]
    [InlineData("registry:HKEY_LOCAL_MACHINE:SOFTWARE")]           // 3 parts — missing ValueName
    [InlineData("registry:HKEY_LOCAL_MACHINE")]                    // 2 parts
    [InlineData("not-registry:HKEY_LOCAL_MACHINE:SOFTWARE:Value")] // wrong prefix
    public void TryResolve_MalformedSource_ReturnsFalse(string source)
    {
        var resolver = new RegistryAppIdResolver(source);

        var result = resolver.TryResolve(@"C:\game.exe", out var appId);

        Assert.False(result);
        Assert.Equal(string.Empty, appId);
    }

    [Fact]
    public void TryResolve_UnknownHiveName_ReturnsFalse()
    {
        var resolver = new RegistryAppIdResolver("registry:HKEY_BOGUS:SOFTWARE:Value");

        var result = resolver.TryResolve(@"C:\game.exe", out _);

        Assert.False(result);
    }

    [Theory]
    [InlineData("registry:HKEY_LOCAL_MACHINE:SOFTWARE\\applanch_test_nonexistent:Value")]
    [InlineData("registry:HKEY_CURRENT_USER:SOFTWARE\\applanch_test_nonexistent:Value")]
    [InlineData("registry:HKEY_CLASSES_ROOT:applanch_test_nonexistent:Value")]
    [InlineData("registry:HKEY_USERS:applanch_test_nonexistent:Value")]
    [InlineData("registry:HKEY_CURRENT_CONFIG:SOFTWARE\\applanch_test_nonexistent:Value")]
    public void TryResolve_ValidHiveButNonExistentKey_ReturnsFalse(string source)
    {
        // Parsing succeeds for all known hive names; registry read returns false because key doesn't exist
        var resolver = new RegistryAppIdResolver(source);

        var result = resolver.TryResolve(@"C:\game.exe", out var appId);

        Assert.False(result);
        Assert.Equal(string.Empty, appId);
    }
}
