using applanch.Infrastructure.Launch.AppIdResolvers;
using applanch.Infrastructure.Utilities;
using applanch.Tests.TestSupport;
using Xunit;

namespace applanch.Tests.Infrastructure.Launch.AppIdResolvers;

public class SteamManifestAppIdResolverTests
{
    private static string WriteSteamLayout(TemporaryDirectory root, string gameDir, string appId)
    {
        var steamApps = Path.Combine(root.Path, "steamapps");
        var common = Path.Combine(steamApps, "common", gameDir);
        Directory.CreateDirectory(common);

        var manifest = Path.Combine(steamApps, $"appmanifest_{appId}.acf");
        File.WriteAllText(manifest,
            $$"""
            "AppState"
            {
                "appid"    "{{appId}}"
                "installdir"    "{{gameDir}}"
            }
            """);

        return Path.Combine(common, "game.exe");
    }

    [Fact]
    public void Resolve_MatchingManifest_ReturnsAppId()
    {
        using var dir = TemporaryDirectory.Create("steam-test");
        var exePath = WriteSteamLayout(dir, "MyGame", "440");
        var resolver = new SteamManifestAppIdResolver();

        var appId = resolver.Resolve(new LaunchPath(exePath));

        Assert.Equal("440", appId);
    }

    [Fact]
    public void CanResolve_NoSteamAppsDirectory_ReturnsFalse()
    {
        using var dir = TemporaryDirectory.Create("steam-test");
        var exePath = Path.Combine(dir.Path, "game.exe");
        File.WriteAllText(exePath, string.Empty);
        var resolver = new SteamManifestAppIdResolver();

        var result = resolver.CanResolve(new LaunchPath(exePath));

        Assert.False(result);
    }

    [Fact]
    public void CanResolve_PathNotUnderCommon_ReturnsFalse()
    {
        using var dir = TemporaryDirectory.Create("steam-test");
        var steamApps = Path.Combine(dir.Path, "steamapps");
        Directory.CreateDirectory(steamApps);
        // Executable is directly under steamapps, not under common/
        var exePath = Path.Combine(steamApps, "game.exe");
        File.WriteAllText(exePath, string.Empty);
        var resolver = new SteamManifestAppIdResolver();

        var result = resolver.CanResolve(new LaunchPath(exePath));

        Assert.False(result);
    }

    [Fact]
    public void Resolve_ManifestInstallDirMismatch_Throws()
    {
        using var dir = TemporaryDirectory.Create("steam-test");
        var steamApps = Path.Combine(dir.Path, "steamapps");
        var exePath = Path.Combine(steamApps, "common", "ActualGameDir", "game.exe");
        Directory.CreateDirectory(Path.GetDirectoryName(exePath)!);
        File.WriteAllText(exePath, string.Empty);
        File.WriteAllText(Path.Combine(steamApps, "appmanifest_999.acf"),
            """
            "AppState"
            {
                "appid"   "999"
                "installdir"   "DifferentGameDir"
            }
            """);
        var resolver = new SteamManifestAppIdResolver();

        Assert.True(resolver.CanResolve(new LaunchPath(exePath)));

        var ex = Assert.Throws<AppIdResolutionException>(() => resolver.Resolve(new LaunchPath(exePath)));

        Assert.Contains("No Steam manifest matched", ex.Message);
    }

    [Fact]
    public void Resolve_ManifestInstalldirIsCaseInsensitive()
    {
        using var dir = TemporaryDirectory.Create("steam-test");
        var exePath = WriteSteamLayout(dir, "mygame", "220");
        // Rewrite manifest with different casing for installdir value
        var steamApps = Path.Combine(dir.Path, "steamapps");
        File.WriteAllText(Path.Combine(steamApps, "appmanifest_220.acf"),
            """
            "AppState"
            {
                "appid"    "220"
                "installdir"    "MYGAME"
            }
            """);
        var resolver = new SteamManifestAppIdResolver();

        var appId = resolver.Resolve(new LaunchPath(exePath));

        Assert.Equal("220", appId);
    }

    [Fact]
    public void Resolve_MalformedAppIdLine_Throws()
    {
        using var dir = TemporaryDirectory.Create("steam-test");
        var steamApps = Path.Combine(dir.Path, "steamapps");
        var exePath = Path.Combine(steamApps, "common", "MyGame", "game.exe");
        Directory.CreateDirectory(Path.GetDirectoryName(exePath)!);
        File.WriteAllText(exePath, string.Empty);
        File.WriteAllText(Path.Combine(steamApps, "appmanifest_440.acf"),
            """
            "AppState"
            {
                "appid"
                "installdir"   "MyGame"
            }
            """);
        var resolver = new SteamManifestAppIdResolver();

        Assert.True(resolver.CanResolve(new LaunchPath(exePath)));

        var ex = Assert.Throws<AppIdResolutionException>(() => resolver.Resolve(new LaunchPath(exePath)));

        Assert.Contains("No Steam manifest matched", ex.Message);
    }
}
