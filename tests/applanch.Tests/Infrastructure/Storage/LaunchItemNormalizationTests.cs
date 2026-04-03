using System.Globalization;
using Xunit;
using applanch.Infrastructure.Storage;

namespace applanch.Tests.Infrastructure.Storage;

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

    [Theory]
    [InlineData("Uncategorized", "en")]
    [InlineData("未分類", "en")]
    [InlineData("Uncategorized", "ja")]
    [InlineData("未分類", "ja")]
    public void NormalizeCategory_KnownDefaultCategoryInAnyLocale_MapsToCurrentDefaultCategory(
        string storedCategory, string activeCulture)
    {
        var previousUiCulture = CultureInfo.CurrentUICulture;
        var previousCulture = CultureInfo.CurrentCulture;
        try
        {
            var culture = new CultureInfo(activeCulture);
            CultureInfo.CurrentUICulture = culture;
            CultureInfo.CurrentCulture = culture;

            var result = LaunchItemNormalization.NormalizeCategory(storedCategory);

            Assert.Equal(LauncherStore.LauncherEntry.DefaultCategory, result);
        }
        finally
        {
            CultureInfo.CurrentUICulture = previousUiCulture;
            CultureInfo.CurrentCulture = previousCulture;
        }
    }

    [Theory]
    [InlineData("  Uncategorized  ", "en")]
    [InlineData("  未分類  ", "en")]
    [InlineData("  Uncategorized  ", "ja")]
    [InlineData("  未分類  ", "ja")]
    public void NormalizeCategory_KnownDefaultCategoryWithWhitespace_MapsToCurrentDefaultCategory(
        string storedCategory, string activeCulture)
    {
        var previousUiCulture = CultureInfo.CurrentUICulture;
        var previousCulture = CultureInfo.CurrentCulture;
        try
        {
            var culture = new CultureInfo(activeCulture);
            CultureInfo.CurrentUICulture = culture;
            CultureInfo.CurrentCulture = culture;

            var result = LaunchItemNormalization.NormalizeCategory(storedCategory);

            Assert.Equal(LauncherStore.LauncherEntry.DefaultCategory, result);
        }
        finally
        {
            CultureInfo.CurrentUICulture = previousUiCulture;
            CultureInfo.CurrentCulture = previousCulture;
        }
    }
}


