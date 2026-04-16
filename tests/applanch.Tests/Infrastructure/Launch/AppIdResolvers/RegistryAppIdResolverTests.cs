using Microsoft.Win32;
using applanch.Infrastructure.Launch.AppIdResolvers;
using Xunit;

namespace applanch.Tests.Infrastructure.Launch.AppIdResolvers;

public class RegistryAppIdResolverTests
{
    [Fact]
    public void Resolve_ExistingCurrentUserValue_ReturnsValue()
    {
        var keyPath = $@"SOFTWARE\applanch_test_{Guid.NewGuid():N}";
        const string valueName = "AppId";
        const string expected = "com.applanch.test";

        try
        {
            using (var key = Registry.CurrentUser.CreateSubKey(keyPath))
            {
                Assert.NotNull(key);
                key.SetValue(valueName, expected, RegistryValueKind.String);
            }

            var source = $"HKEY_CURRENT_USER:{keyPath}:{valueName}";
            var resolver = new RegistryAppIdResolver(source);

            var actual = resolver.Resolve(new LaunchPath(@"C:\game.exe"));

            Assert.Equal(expected, actual);
        }
        finally
        {
            Registry.CurrentUser.DeleteSubKeyTree(keyPath, throwOnMissingSubKey: false);
        }
    }

    [Theory]
    [InlineData(" HKEY_LOCAL_MACHINE : SOFTWARE : Value ")]
    [InlineData("HKEY_LOCAL_MACHINE:SOFTWARE:Value:With:Colon")]
    public void CanResolve_ValidSource_ReturnsTrue(string source)
    {
        var resolver = new RegistryAppIdResolver(source);

        var result = resolver.CanResolve(new LaunchPath(@"C:\game.exe"));

        Assert.True(result);
    }

    [Theory]
    [InlineData("HKEY_LOCAL_MACHINE:SOFTWARE")]           // 2 parts
    [InlineData("HKEY_LOCAL_MACHINE")]                    // 1 part
    [InlineData("registry:HKEY_LOCAL_MACHINE:SOFTWARE:Value")] // prefix should not be included for this resolver
    [InlineData("hkey_current_user:SOFTWARE:Value")]      // hive is case-sensitive
    public void CanResolve_MalformedSource_ReturnsFalse(string source)
    {
        var resolver = new RegistryAppIdResolver(source);

        var result = resolver.CanResolve(new LaunchPath(@"C:\game.exe"));

        Assert.False(result);
    }

    [Fact]
    public void CanResolve_UnknownHiveName_ReturnsFalse()
    {
        var resolver = new RegistryAppIdResolver("HKEY_BOGUS:SOFTWARE:Value");

        var result = resolver.CanResolve(new LaunchPath(@"C:\game.exe"));

        Assert.False(result);
    }

    [Theory]
    [InlineData("HKEY_LOCAL_MACHINE:SOFTWARE\\applanch_test_nonexistent:Value")]
    [InlineData("HKEY_CURRENT_USER:SOFTWARE\\applanch_test_nonexistent:Value")]
    [InlineData("HKEY_CLASSES_ROOT:applanch_test_nonexistent:Value")]
    [InlineData("HKEY_USERS:applanch_test_nonexistent:Value")]
    [InlineData("HKEY_CURRENT_CONFIG:SOFTWARE\\applanch_test_nonexistent:Value")]
    public void Resolve_ValidHiveButNonExistentKey_Throws(string source)
    {
        var resolver = new RegistryAppIdResolver(source);

        var ex = Assert.Throws<AppIdResolutionException>(() => resolver.Resolve(new LaunchPath(@"C:\game.exe")));

        Assert.Contains("Registry key", ex.Message);
        Assert.Contains("was not found", ex.Message);
    }
}
