using applanch.Infrastructure.Launch;
using applanch.Configuration;
using Xunit;

namespace applanch.Tests.Infrastructure.Launch;

public sealed class LaunchFallbackConfigurationLoaderTests
{
    [Fact]
    public void LoadFromDirectory_DoesNotCreateUserDefinedDirectory()
    {
        var root = CreateTempDirectory();
        var appBase = Path.Combine(root, "appbase");
        Directory.CreateDirectory(Path.Combine(appBase, "Config"));
        File.WriteAllText(Path.Combine(appBase, "Config", "launch-fallbacks.json"), "{\"rules\":[]}");

        try
        {
            _ = LaunchFallbackConfigurationLoader.LoadFromDirectory(appBase);

            var userDefinedDirectory = Path.Combine(appBase, "Config", "UserDefined", "launch-fallbacks");
            Assert.False(Directory.Exists(userDefinedDirectory));
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public void LoadFromDirectory_LoadsBundledAndUserDefinedRules()
    {
        var root = CreateTempDirectory();
        var appBase = Path.Combine(root, "appbase");
        Directory.CreateDirectory(Path.Combine(appBase, "Config"));

        var bundledPath = Path.Combine(appBase, "Config", "launch-fallbacks.json");
        File.WriteAllText(bundledPath, "{\"rules\":[{\"name\":\"Bundled\",\"kind\":\"uri-template\",\"uriTemplate\":\"bundled://{appId}\"}]}");

        var userDefinedDirectory = Path.Combine(appBase, "Config", "UserDefined", "launch-fallbacks");
        Directory.CreateDirectory(userDefinedDirectory);
        var userDefinedPath = Path.Combine(userDefinedDirectory, "custom.json");
        File.WriteAllText(userDefinedPath, "{\"rules\":[{\"name\":\"Custom\",\"kind\":\"uri-template\",\"uriTemplate\":\"custom://{appId}\"}]}");

        try
        {
            var config = LaunchFallbackConfigurationLoader.LoadFromDirectory(appBase);

            Assert.Contains(config.Rules, static x => x.Name == "Bundled");
            Assert.Contains(config.Rules, static x => x.Name == "Custom");
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public void LoadFromDirectory_WhenNoBundledOrUserDefined_ReturnsEmpty()
    {
        using var scope = BundledConfigLoadNotificationTestScope.Enter();
        var root = CreateTempDirectory();
        var appBase = Path.Combine(root, "appbase");
        Directory.CreateDirectory(Path.Combine(appBase, "Config"));

        try
        {
            var config = LaunchFallbackConfigurationLoader.LoadFromDirectory(appBase);

            Assert.Empty(config.Rules);
            Assert.Contains(
                BundledConfigLoadNotificationCenter.DrainPending(),
                static issue => issue == new BundledConfigLoadIssue("launch-fallbacks.json", IsInvalidFormat: false));
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public void LoadFromDirectory_WhenBundledJsonIsInvalid_ReportsInvalidFormat()
    {
        using var scope = BundledConfigLoadNotificationTestScope.Enter();
        var root = CreateTempDirectory();
        var appBase = Path.Combine(root, "appbase");
        Directory.CreateDirectory(Path.Combine(appBase, "Config"));
        File.WriteAllText(Path.Combine(appBase, "Config", "launch-fallbacks.json"), "{");

        try
        {
            var config = LaunchFallbackConfigurationLoader.LoadFromDirectory(appBase);

            Assert.Empty(config.Rules);
            Assert.Contains(
                BundledConfigLoadNotificationCenter.DrainPending(),
                static issue => issue == new BundledConfigLoadIssue("launch-fallbacks.json", IsInvalidFormat: true));
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    private static string CreateTempDirectory()
    {
        var path = Path.Combine(Path.GetTempPath(), "applanch-loader-tests-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(path);
        return path;
    }
}

