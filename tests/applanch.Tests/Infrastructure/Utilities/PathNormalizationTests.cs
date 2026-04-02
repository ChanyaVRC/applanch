using applanch.Infrastructure.Utilities;
using Microsoft.Win32;
using Xunit;

namespace applanch.Tests.Infrastructure.Utilities;

public class PathNormalizationTests
{
    [Fact]
    public void NormalizeForComparison_TrimsAndRemovesTrailingDirectorySeparator()
    {
        var tempDir = Path.GetTempPath().TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        var input = $"  {tempDir}{Path.DirectorySeparatorChar}  ";

        var normalized = PathNormalization.NormalizeForComparison(input);

        Assert.Equal(tempDir, normalized);
    }

    [Fact]
    public void NormalizeDirectoryPath_DriveSpecifier_AddsTrailingSeparator()
    {
        var driveRoot = Path.GetPathRoot(Path.GetTempPath());
        Assert.False(string.IsNullOrWhiteSpace(driveRoot));
        var driveSpecifier = driveRoot!.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);

        var normalized = PathNormalization.NormalizeDirectoryPath(driveSpecifier);

        Assert.Equal(driveRoot, normalized);
    }

    [Fact]
    public void TryNormalizePersistablePath_RelativePath_ReturnsFalse()
    {
        var success = PathNormalization.TryNormalizePersistablePath(@"tools\app.exe", out var normalized);

        Assert.False(success);
        Assert.Equal(string.Empty, normalized);
    }

    [Theory]
    [InlineData("https://example.com")]
    [InlineData("http://example.com/path")]
    public void TryNormalizePersistablePath_Url_StoredAsIs(string url)
    {
        var success = PathNormalization.TryNormalizePersistablePath(url, out var normalized);

        Assert.True(success);
        Assert.Equal(url, normalized);
    }

    [Theory]
    [InlineData("https://example.com")]
    [InlineData("http://example.com/path")]
    public void IsUrl_RegisteredScheme_ReturnsTrue(string url)
    {
        Assert.True(PathNormalization.IsUrl(url));
    }

    [Theory]
    [InlineData("https://example.com")]
    [InlineData("http://example.com/path")]
    public void IsHttpUrl_HttpSchemes_ReturnTrue(string url)
    {
        Assert.True(PathNormalization.IsHttpUrl(url));
        Assert.True(PathNormalization.TryParseHttpUrl(url, out var uri));
        Assert.Equal(new Uri(url).AbsoluteUri, uri.AbsoluteUri);
    }

    [Theory]
    [InlineData("steam://run/123")]
    [InlineData("mailto:user@example.com")]
    public void IsHttpUrl_NonHttpSchemes_ReturnFalse(string url)
    {
        Assert.False(PathNormalization.IsHttpUrl(url));
        Assert.False(PathNormalization.TryParseHttpUrl(url, out _));
    }

    [Fact]
    public void NormalizeLaunchPath_HttpUrl_TrimsAndKeepsUrl()
    {
        var normalized = PathNormalization.NormalizeLaunchPath("  https://example.com/path?q=1  ");

        Assert.Equal("https://example.com/path?q=1", normalized);
    }

    [Fact]
    public void NormalizeLaunchPath_FilePath_RemovesTrailingDirectorySeparator()
    {
        var tempDir = Path.GetTempPath().TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        var input = $"  {tempDir}{Path.DirectorySeparatorChar}  ";

        var normalized = PathNormalization.NormalizeLaunchPath(input);

        Assert.Equal(tempDir, normalized);
    }

    [Theory]
    [InlineData("https://example.com", 2)]
    [InlineData("http://example.com/path", 2)]
    [InlineData(@"C:\Tools\app.exe", 0)]
    [InlineData(@"tools\app.exe", 0)]
    public void GetPathType_ClassifiesCommonInputs(string input, int expected)
    {
        var actual = PathNormalization.GetPathType(input);

        Assert.Equal((PathType)expected, actual);
    }

    [Fact]
    public void GetPathType_CustomRegisteredScheme_ReturnsRegisteredUrl()
    {
        var scheme = "applanch-test-" + Guid.NewGuid().ToString("N")[..8];
        var keyPath = $@"Software\Classes\{scheme}";
        try
        {
            using var key = Registry.CurrentUser.CreateSubKey(keyPath);
            key.SetValue("URL Protocol", string.Empty);

            var url = $"{scheme}://something";
            var pathType = PathNormalization.GetPathType(url, out var uri);

            Assert.Equal(PathType.RegisteredUrl, pathType);
            Assert.NotNull(uri);
            Assert.Equal(new Uri(url).AbsoluteUri, uri!.AbsoluteUri);
        }
        finally
        {
            Registry.CurrentUser.DeleteSubKeyTree(keyPath, throwOnMissingSubKey: false);
        }
    }

    [Fact]
    public void IsUrl_UnregisteredScheme_ReturnsFalse()
    {
        Assert.False(PathNormalization.IsUrl("invalidscheme://something"));
    }

    [Fact]
    public void IsUrl_CustomRegisteredScheme_ReturnsTrue()
    {
        var scheme = "applanch-test-" + Guid.NewGuid().ToString("N")[..8];
        var keyPath = $@"Software\Classes\{scheme}";
        try
        {
            using var key = Registry.CurrentUser.CreateSubKey(keyPath);
            key.SetValue("URL Protocol", string.Empty);

            Assert.True(PathNormalization.IsUrl($"{scheme}://something"));
        }
        finally
        {
            Registry.CurrentUser.DeleteSubKeyTree(keyPath, throwOnMissingSubKey: false);
        }
    }

    [Theory]
    [InlineData(@"C:\Tools\app.exe")]
    [InlineData(@"tools\app.exe")]
    [InlineData("foo:bar")]
    [InlineData("CON:something")]
    [InlineData("invalidscheme://something")]
    public void IsUrl_FilePath_ReturnsFalse(string path)
    {
        Assert.False(PathNormalization.IsUrl(path));
    }
}

