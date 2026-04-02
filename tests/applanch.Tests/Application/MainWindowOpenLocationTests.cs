using applanch.Infrastructure.Utilities;
using applanch.Tests.TestSupport;
using Xunit;

namespace applanch.Tests.Application;

public class MainWindowOpenLocationTests
{
    [Fact]
    public void TryCreateOpenLocationStartInfo_ExistingFile_ReturnsExplorerSelectArguments()
    {
        using var tempDirectory = TemporaryDirectory.Create();
        var filePath = Path.Combine(tempDirectory.Path, "tool.exe");
        File.WriteAllText(filePath, string.Empty);

        var canOpen = MainWindow.TryCreateOpenLocationStartInfo(new LaunchPath(filePath), out var startInfo);

        Assert.True(canOpen);
        Assert.Equal("explorer.exe", startInfo.FileName);
        Assert.Equal($"/select,\"{filePath}\"", startInfo.Arguments);
        Assert.True(startInfo.UseShellExecute);
    }

    [Fact]
    public void TryCreateOpenLocationStartInfo_ExistingDirectory_ReturnsExplorerDirectoryArguments()
    {
        using var tempDirectory = TemporaryDirectory.Create();

        var canOpen = MainWindow.TryCreateOpenLocationStartInfo(new LaunchPath(tempDirectory.Path), out var startInfo);

        Assert.True(canOpen);
        Assert.Equal("explorer.exe", startInfo.FileName);
        Assert.Equal($"\"{tempDirectory.Path}\"", startInfo.Arguments);
        Assert.True(startInfo.UseShellExecute);
    }

    [Fact]
    public void TryCreateOpenLocationStartInfo_MissingPath_ReturnsFalse()
    {
        var canOpen = MainWindow.TryCreateOpenLocationStartInfo(new LaunchPath(@"C:\\this\\path\\does-not-exist"), out _);

        Assert.False(canOpen);
    }

    [Fact]
    public void TryCreateOpenLocationStartInfo_Url_ReturnsFalse()
    {
        var canOpen = MainWindow.TryCreateOpenLocationStartInfo(new LaunchPath("https://example.com"), out _);

        Assert.False(canOpen);
    }

    [Fact]
    public void ShouldOfferDeleteActionForMissingPath_MissingPath_ReturnsTrue()
    {
        var shouldOffer = MainWindow.ShouldOfferDeleteActionForMissingPath(new LaunchPath(@"C:\\this\\path\\does-not-exist"));

        Assert.True(shouldOffer);
    }

    [Fact]
    public void ShouldOfferDeleteActionForMissingPath_Url_ReturnsFalse()
    {
        var shouldOffer = MainWindow.ShouldOfferDeleteActionForMissingPath(new LaunchPath("https://example.com"));

        Assert.False(shouldOffer);
    }

    [Fact]
    public void ShouldOfferDeleteActionForMissingPath_ExistingFile_ReturnsFalse()
    {
        using var tempDirectory = TemporaryDirectory.Create();
        var filePath = Path.Combine(tempDirectory.Path, "tool.exe");
        File.WriteAllText(filePath, string.Empty);

        var shouldOffer = MainWindow.ShouldOfferDeleteActionForMissingPath(new LaunchPath(filePath));

        Assert.False(shouldOffer);
    }
}
