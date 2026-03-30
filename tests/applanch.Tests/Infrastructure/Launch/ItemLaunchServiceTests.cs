using Xunit;
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
}


