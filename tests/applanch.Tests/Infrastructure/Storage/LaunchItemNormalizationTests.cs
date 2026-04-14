using Xunit;
using applanch.Infrastructure.Storage;
using applanch.Tests.TestSupport;

namespace applanch.Tests.Infrastructure.Storage;

public class LaunchItemNormalizationTests
{
    public static TheoryData<string, string> NormalizeCategoryCases =>
        new()
        {
            { "   ", LauncherEntry.DefaultCategory },
            { "  Dev  ", "Dev" },
        };

    [Theory]
    [MemberData(nameof(NormalizeCategoryCases))]
    public void NormalizeCategory_ReturnsExpectedValue(string input, string expected)
    {
        var result = LaunchItemNormalization.NormalizeCategory(input);

        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData(null, "")]
    [InlineData("   ", "")]
    [InlineData("  --run  ", "--run")]
    public void NormalizeArguments_ReturnsExpectedValue(string? input, string expected)
    {
        var result = LaunchItemNormalization.NormalizeArguments(input);

        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData(" ", @"C:\Tools\MyApp.exe", "MyApp")]
    [InlineData("  Custom Name  ", @"C:\Tools\MyApp.exe", "Custom Name")]
    public void NormalizeDisplayName_ReturnsExpectedValue(string inputName, string launchPath, string expected)
    {
        var result = LaunchItemNormalization.NormalizeDisplayName(inputName, launchPath);

        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("Uncategorized", "en")]
    [InlineData("未分類", "en")]
    [InlineData("Uncategorized", "ja")]
    [InlineData("未分類", "ja")]
    public void NormalizeCategory_KnownDefaultCategoryInAnyLocale_MapsToCurrentDefaultCategory(
        string storedCategory, string activeCulture)
    {
        using var cultureScope = new CultureScope(activeCulture);

        var result = LaunchItemNormalization.NormalizeCategory(storedCategory);

        Assert.Equal(LauncherEntry.DefaultCategory, result);
    }

    [Theory]
    [InlineData("  Uncategorized  ", "en")]
    [InlineData("  未分類  ", "en")]
    [InlineData("  Uncategorized  ", "ja")]
    [InlineData("  未分類  ", "ja")]
    public void NormalizeCategory_KnownDefaultCategoryWithWhitespace_MapsToCurrentDefaultCategory(
        string storedCategory, string activeCulture)
    {
        using var cultureScope = new CultureScope(activeCulture);

        var result = LaunchItemNormalization.NormalizeCategory(storedCategory);

        Assert.Equal(LauncherEntry.DefaultCategory, result);
    }
}


