using applanch.Infrastructure.Utilities;
using Xunit;

namespace applanch.Tests.Infrastructure.Utilities;

public class LaunchPathTests
{
    [Fact]
    public void Constructor_TrimsAndClassifiesHttpUrl()
    {
        var path = new LaunchPath("  https://example.com/path?q=1  ");

        Assert.Equal("https://example.com/path?q=1", path.Value);
        Assert.Equal(PathType.HttpUrl, path.Type);
        Assert.True(path.IsUrl);
        Assert.True(path.IsHttpUrl);
    }

    [Fact]
    public void Constructor_Empty_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() => new LaunchPath("   "));
    }

    [Fact]
    public void Equals_IsCaseInsensitiveForFilePaths()
    {
        var left = new LaunchPath(@"C:\Tools\App.exe");
        var right = new LaunchPath(@"c:\tools\APP.exe");

        Assert.Equal(left, right);
        Assert.True(left == right);
    }

    [Fact]
    public void TryCreate_Empty_ReturnsFalse()
    {
        var created = LaunchPath.TryCreate(" ", out var path);

        Assert.False(created);
        Assert.Equal(default, path);
    }
}
