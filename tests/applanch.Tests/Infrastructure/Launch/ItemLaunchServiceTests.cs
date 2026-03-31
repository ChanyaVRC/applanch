using Xunit;
using System.ComponentModel;
using System.Diagnostics;
using applanch.Infrastructure.Launch;
using applanch.Tests.Infrastructure.Launch.TestDoubles;
using applanch.Tests.TestSupport;

namespace applanch.Tests.Infrastructure.Launch;

public class ItemLaunchServiceTests
{
    [Fact]
    public void TryLaunch_MissingPath_ReturnsWarningFailure()
    {
        var launcher = new FakeProcessLauncher();
        var service = new ItemLaunchService(launcher.Start);
        var item = new LaunchItemViewModel(@"C:\\missing\\file.exe", "Dev", string.Empty, "Missing");

        var result = service.TryLaunch(item);

        Assert.False(result.IsSuccess);
        Assert.Equal(System.Windows.MessageBoxImage.Warning, result.Icon);
    }

    [Fact]
    public void TryLaunch_FilePath_UsesFileAndArguments()
    {
        using var tempDirectory = TemporaryDirectory.Create();
        var filePath = Path.Combine(tempDirectory.Path, "app.exe");
        File.WriteAllText(filePath, string.Empty);

        var launcher = new FakeProcessLauncher();
        var service = new ItemLaunchService(launcher.Start);
        var item = new LaunchItemViewModel(filePath, "Dev", "--flag", "App");

        var result = service.TryLaunch(item);

        Assert.True(result.IsSuccess);
        Assert.NotNull(launcher.LastStartInfo);
        Assert.Equal(filePath, launcher.LastStartInfo!.FileName);
        Assert.Equal("--flag", launcher.LastStartInfo.Arguments);
        Assert.True(launcher.LastStartInfo.UseShellExecute);
    }

    [Fact]
    public void TryLaunch_Directory_UsesExplorer()
    {
        using var tempDirectory = TemporaryDirectory.Create();
        var launcher = new FakeProcessLauncher();
        var service = new ItemLaunchService(launcher.Start);
        var item = new LaunchItemViewModel(tempDirectory.Path, "Dev", string.Empty, "Dir");

        var result = service.TryLaunch(item);

        Assert.True(result.IsSuccess);
        Assert.NotNull(launcher.LastStartInfo);
        Assert.Equal("explorer.exe", launcher.LastStartInfo!.FileName);
        Assert.Equal($"\"{tempDirectory.Path}\"", launcher.LastStartInfo.Arguments);
    }

    [Fact]
    public void TryLaunch_WhenLauncherThrows_ReturnsErrorFailure()
    {
        using var tempDirectory = TemporaryDirectory.Create();
        var filePath = Path.Combine(tempDirectory.Path, "app.exe");
        File.WriteAllText(filePath, string.Empty);

        var launcher = new FakeProcessLauncher { ThrowOnStart = true };
        var service = new ItemLaunchService(launcher.Start);
        var item = new LaunchItemViewModel(filePath, "Dev", string.Empty, "App");

        var result = service.TryLaunch(item);

        Assert.False(result.IsSuccess);
        Assert.Equal(System.Windows.MessageBoxImage.Error, result.Icon);
    }

    [Fact]
    public void TryLaunch_WhenLauncherReturnsNull_ReturnsErrorFailure()
    {
        using var tempDirectory = TemporaryDirectory.Create();
        var filePath = Path.Combine(tempDirectory.Path, "app.exe");
        File.WriteAllText(filePath, string.Empty);

        var launcher = new FakeProcessLauncher { ReturnNull = true };
        var service = new ItemLaunchService(launcher.Start);
        var item = new LaunchItemViewModel(filePath, "Dev", string.Empty, "App");

        var result = service.TryLaunch(item);

        Assert.False(result.IsSuccess);
        Assert.Equal(System.Windows.MessageBoxImage.Error, result.Icon);
    }

    [Fact]
    public void TryLaunch_RunAsAdministrator_SetsRunAsVerb()
    {
        using var tempDirectory = TemporaryDirectory.Create();
        var filePath = Path.Combine(tempDirectory.Path, "app.exe");
        File.WriteAllText(filePath, string.Empty);

        var launcher = new FakeProcessLauncher();
        var service = new ItemLaunchService(launcher.Start);
        var item = new LaunchItemViewModel(filePath, "Dev", string.Empty, "App");

        var result = service.TryLaunch(item, runAsAdministrator: true);

        Assert.True(result.IsSuccess);
        Assert.NotNull(launcher.LastStartInfo);
        Assert.Equal("runas", launcher.LastStartInfo!.Verb);
    }

    [Fact]
    public void TryLaunch_ValorantAccessDenied_FallsBackToRiotClient()
    {
        using var tempDirectory = TemporaryDirectory.Create();
        var valorantPath = Path.Combine(tempDirectory.Path, "Riot Games", "VALORANT", "live", "VALORANT.exe");
        var riotClientPath = Path.Combine(tempDirectory.Path, "Riot Games", "Riot Client", "RiotClientServices.exe");
        Directory.CreateDirectory(Path.GetDirectoryName(valorantPath)!);
        Directory.CreateDirectory(Path.GetDirectoryName(riotClientPath)!);
        File.WriteAllText(valorantPath, string.Empty);
        File.WriteAllText(riotClientPath, string.Empty);

        var attempts = new List<ProcessStartInfo>();
        Process? Launcher(ProcessStartInfo startInfo)
        {
            attempts.Add(startInfo);
            if (attempts.Count == 1)
            {
                throw new Win32Exception(5, "Access is denied");
            }

            return new Process();
        }

        var service = new ItemLaunchService(Launcher);
        var item = new LaunchItemViewModel(valorantPath, "Games", string.Empty, "VALORANT");

        var result = service.TryLaunch(item);

        Assert.True(result.IsSuccess);
        Assert.Equal(2, attempts.Count);
        Assert.Equal(valorantPath, attempts[0].FileName);
        Assert.Equal(riotClientPath, attempts[1].FileName);
        Assert.Equal("--launch-product=valorant --launch-patchline=live", attempts[1].Arguments);
    }

    [Fact]
    public void TryLaunch_SteamLibraryAccessDenied_FallsBackToSteamUri()
    {
        using var tempDirectory = TemporaryDirectory.Create();
        var steamRoot = Path.Combine(tempDirectory.Path, "Steam");
        var steamApps = Path.Combine(steamRoot, "steamapps");
        var gameDirectory = Path.Combine(steamApps, "common", "CoolGame");
        var gamePath = Path.Combine(gameDirectory, "coolgame.exe");
        var manifest = Path.Combine(steamApps, "appmanifest_12345.acf");

        Directory.CreateDirectory(gameDirectory);
        File.WriteAllText(gamePath, string.Empty);
        File.WriteAllText(manifest,
            "\"AppState\"\n" +
            "{\n" +
            "  \"appid\"  \"12345\"\n" +
            "  \"installdir\"  \"CoolGame\"\n" +
            "}\n");

        var attempts = new List<ProcessStartInfo>();
        Process? Launcher(ProcessStartInfo startInfo)
        {
            attempts.Add(startInfo);
            if (attempts.Count == 1)
            {
                throw new Win32Exception(5, "Access is denied");
            }

            return new Process();
        }

        var service = new ItemLaunchService(Launcher);
        var item = new LaunchItemViewModel(gamePath, "Games", string.Empty, "CoolGame");

        var result = service.TryLaunch(item);

        Assert.True(result.IsSuccess);
        Assert.Equal(2, attempts.Count);
        Assert.Equal(gamePath, attempts[0].FileName);
        Assert.Equal("steam://rungameid/12345", attempts[1].FileName);
        Assert.Equal(string.Empty, attempts[1].Arguments);
    }
}


