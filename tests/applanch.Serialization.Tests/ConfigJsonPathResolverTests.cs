using Xunit;

namespace applanch.Serialization.Tests;

public sealed class ConfigJsonPathResolverTests
{
    [Fact]
    public void EnumerateBundledAndUserDefined_ReturnsBundledEntryFirstWithMetadata()
    {
        var root = CreateTempDirectory();
        var appBase = Path.Combine(root, "appbase");
        var configDirectory = Path.Combine(appBase, "Config");
        var userDefinedDirectory = Path.Combine(configDirectory, "UserDefined", "icon-path-mappings");
        Directory.CreateDirectory(userDefinedDirectory);

        File.WriteAllText(Path.Combine(configDirectory, "icon-path-mappings.json"), "{}");
        File.WriteAllText(Path.Combine(userDefinedDirectory, "b.json"), "{}");
        File.WriteAllText(Path.Combine(userDefinedDirectory, "a.json"), "{}");

        try
        {
            var paths = ConfigJsonPathResolver
                .EnumerateBundledAndUserDefined(appBase, "icon-path-mappings.json", "icon-path-mappings")
                .ToArray();

            Assert.Collection(
                paths,
                entry =>
                {
                    Assert.True(entry.IsBundled);
                    Assert.Equal(Path.Combine(configDirectory, "icon-path-mappings.json"), entry.Path);
                },
                entry =>
                {
                    Assert.False(entry.IsBundled);
                    Assert.Equal(Path.Combine(userDefinedDirectory, "a.json"), entry.Path);
                },
                entry =>
                {
                    Assert.False(entry.IsBundled);
                    Assert.Equal(Path.Combine(userDefinedDirectory, "b.json"), entry.Path);
                });
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    private static string CreateTempDirectory()
    {
        var path = Path.Combine(Path.GetTempPath(), "applanch-config-path-resolver-tests-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(path);
        return path;
    }
}
