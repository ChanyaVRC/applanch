using applanch.Infrastructure.Integration;
using applanch.Core.Configuration;
using Xunit;

namespace applanch.Tests.Infrastructure.Integration;

public sealed class LaunchItemIconPathMappingConfigurationLoaderTests
{
    [Fact]
    public void LoadFromDirectory_LoadsBundledAndUserDefinedRules()
    {
        var root = CreateTempDirectory();
        var appBase = Path.Combine(root, "appbase");
        var configDirectory = Path.Combine(appBase, "Config");
        Directory.CreateDirectory(configDirectory);

        File.WriteAllText(
            Path.Combine(configDirectory, "icon-path-mappings.json"),
            "{\"rules\":[{\"name\":\"Bundled\",\"matchFileNames\":[\"Update.exe\"],\"iconPathTemplate\":\"C:\\\\\\\\Discord.exe\"}]}");

        var userDefinedDirectory = Path.Combine(configDirectory, "UserDefined", "icon-path-mappings");
        Directory.CreateDirectory(userDefinedDirectory);
        File.WriteAllText(
            Path.Combine(userDefinedDirectory, "custom.json"),
            "{\"rules\":[{\"name\":\"Custom\",\"matchFileNames\":[\"Update.exe\"],\"iconPathTemplate\":\"D:\\\\\\\\Discord.exe\"}]}");

        try
        {
            var config = LaunchItemIconPathMappingConfigurationLoader.LoadFromDirectory(appBase);

            Assert.Contains(config.Rules, static rule => rule.Name == "Bundled");
            Assert.Contains(config.Rules, static rule => rule.Name == "Custom");
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public void LoadFromDirectory_WhenBundledConfigMissing_ReportsMissing()
    {
        using var scope = BundledConfigLoadNotificationTestScope.Enter();
        var root = CreateTempDirectory();
        var appBase = Path.Combine(root, "appbase");
        Directory.CreateDirectory(Path.Combine(appBase, "Config"));

        try
        {
            var config = LaunchItemIconPathMappingConfigurationLoader.LoadFromDirectory(appBase);

            Assert.Empty(config.Rules);
            Assert.Contains(
                BundledConfigLoadNotificationCenter.DrainPending(),
                static issue => issue == new BundledConfigLoadIssue("icon-path-mappings.json", IsInvalidFormat: false));
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public void LoadFromDirectory_WhenBundledConfigIsInvalid_ReportsInvalidFormat()
    {
        using var scope = BundledConfigLoadNotificationTestScope.Enter();
        var root = CreateTempDirectory();
        var appBase = Path.Combine(root, "appbase");
        var configDirectory = Path.Combine(appBase, "Config");
        Directory.CreateDirectory(configDirectory);
        File.WriteAllText(Path.Combine(configDirectory, "icon-path-mappings.json"), "{");

        try
        {
            var config = LaunchItemIconPathMappingConfigurationLoader.LoadFromDirectory(appBase);

            Assert.Empty(config.Rules);
            Assert.Contains(
                BundledConfigLoadNotificationCenter.DrainPending(),
                static issue => issue == new BundledConfigLoadIssue("icon-path-mappings.json", IsInvalidFormat: true));
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    private static string CreateTempDirectory()
    {
        var path = Path.Combine(Path.GetTempPath(), "applanch-icon-map-loader-tests-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(path);
        return path;
    }
}
