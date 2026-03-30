using Xunit;
using applanch.Infrastructure.Storage;

namespace applanch.Tests;

public class LaunchItemNormalizationTests
{
    [Fact]
    public void NormalizeCategory_Whitespace_ReturnsDefaultCategory()
    {
        var result = LaunchItemNormalization.NormalizeCategory("   ");

        Assert.Equal(LauncherStore.LauncherEntry.DefaultCategory, result);
    }

    [Fact]
    public void NormalizeCategory_TrimsText()
    {
        var result = LaunchItemNormalization.NormalizeCategory("  Dev  ");

        Assert.Equal("Dev", result);
    }

    [Fact]
    public void NormalizeArguments_NullOrWhitespace_ReturnsEmpty()
    {
        Assert.Equal(string.Empty, LaunchItemNormalization.NormalizeArguments(null));
        Assert.Equal(string.Empty, LaunchItemNormalization.NormalizeArguments("   "));
    }

    [Fact]
    public void NormalizeArguments_TrimsText()
    {
        var result = LaunchItemNormalization.NormalizeArguments("  --run  ");

        Assert.Equal("--run", result);
    }

    [Fact]
    public void NormalizeDisplayName_Whitespace_UsesPathFileNameWithoutExtension()
    {
        var result = LaunchItemNormalization.NormalizeDisplayName(" ", @"C:\\Tools\\MyApp.exe");

        Assert.Equal("MyApp", result);
    }

    [Fact]
    public void NormalizeDisplayName_TrimsExplicitName()
    {
        var result = LaunchItemNormalization.NormalizeDisplayName("  Custom Name  ", @"C:\\Tools\\MyApp.exe");

        Assert.Equal("Custom Name", result);
    }
}

