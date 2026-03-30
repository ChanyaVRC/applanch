using System.Diagnostics;
using Xunit;
using applanch.Infrastructure.Launch;

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
        var tempDir = CreateTempDirectory();
        try
        {
            var filePath = Path.Combine(tempDir, "app.exe");
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
        finally
        {
            Directory.Delete(tempDir, recursive: true);
        }
    }

    [Fact]
    public void TryLaunch_Directory_UsesExplorer()
    {
        var tempDir = CreateTempDirectory();
        try
        {
            var launcher = new FakeProcessLauncher();
            var service = new ItemLaunchService(launcher.Start);
            var item = new LaunchItemViewModel(tempDir, "Dev", string.Empty, "Dir");

            var result = service.TryLaunch(item);

            Assert.True(result.IsSuccess);
            Assert.NotNull(launcher.LastStartInfo);
            Assert.Equal("explorer.exe", launcher.LastStartInfo!.FileName);
            Assert.Equal($"\"{tempDir}\"", launcher.LastStartInfo.Arguments);
        }
        finally
        {
            Directory.Delete(tempDir, recursive: true);
        }
    }

    [Fact]
    public void TryLaunch_WhenLauncherThrows_ReturnsErrorFailure()
    {
        var tempDir = CreateTempDirectory();
        try
        {
            var filePath = Path.Combine(tempDir, "app.exe");
            File.WriteAllText(filePath, string.Empty);

            var launcher = new FakeProcessLauncher { ThrowOnStart = true };
            var service = new ItemLaunchService(launcher.Start);
            var item = new LaunchItemViewModel(filePath, "Dev", string.Empty, "App");

            var result = service.TryLaunch(item);

            Assert.False(result.IsSuccess);
            Assert.Equal(System.Windows.MessageBoxImage.Error, result.Icon);
        }
        finally
        {
            Directory.Delete(tempDir, recursive: true);
        }
    }

    [Fact]
    public void TryLaunch_WhenLauncherReturnsNull_ReturnsErrorFailure()
    {
        var tempDir = CreateTempDirectory();
        try
        {
            var filePath = Path.Combine(tempDir, "app.exe");
            File.WriteAllText(filePath, string.Empty);

            var launcher = new FakeProcessLauncher { ReturnNull = true };
            var service = new ItemLaunchService(launcher.Start);
            var item = new LaunchItemViewModel(filePath, "Dev", string.Empty, "App");

            var result = service.TryLaunch(item);

            Assert.False(result.IsSuccess);
            Assert.Equal(System.Windows.MessageBoxImage.Error, result.Icon);
        }
        finally
        {
            Directory.Delete(tempDir, recursive: true);
        }
    }

    private static string CreateTempDirectory()
    {
        var path = Path.Combine(Path.GetTempPath(), "applanch-tests-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(path);
        return path;
    }

    private sealed class FakeProcessLauncher
    {
        public bool ThrowOnStart { get; set; }
        public bool ReturnNull { get; set; }
        public ProcessStartInfo? LastStartInfo { get; private set; }

        public Process? Start(ProcessStartInfo startInfo)
        {
            LastStartInfo = startInfo;

            if (ThrowOnStart)
            {
                throw new InvalidOperationException("simulated");
            }

            if (ReturnNull)
            {
                return null;
            }

            return new Process();
        }
    }
}


