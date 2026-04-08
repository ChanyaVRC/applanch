using applanch.Infrastructure.Storage;
using applanch.Tests.TestSupport;
using Xunit;

namespace applanch.Tests.Infrastructure.Storage;

public class CategoryTests
{
    [Fact]
    public void Default_IsEmpty_AndUsesDefaultDisplayLabel()
    {
        var category = Category.Default;

        Assert.True(category.IsDefault);
        Assert.False(category.IsAll);
        Assert.Equal(string.Empty, category.Value);
        Assert.Equal(AppResources.DefaultCategory, category.ToDisplayLabel());
    }

    [Fact]
    public void All_EqualsAllCategoriesConstant_AndUsesAllCategoriesDisplayLabel()
    {
        var category = Category.All;

        Assert.True(category.IsAll);
        Assert.False(category.IsDefault);
        Assert.Equal(LauncherEntry.AllCategories, category.Value);
        Assert.Equal(AppResources.AllCategories, category.ToDisplayLabel());
    }

    [Theory]
    [InlineData("Uncategorized", "en")]
    [InlineData("未分類", "en")]
    [InlineData("Uncategorized", "ja")]
    [InlineData("未分類", "ja")]
    public void FromInput_KnownDefaultLabels_MapsToDefaultCategory(string rawCategory, string culture)
    {
        using var scope = new CultureScope(culture);

        var category = Category.FromInput(rawCategory);

        Assert.True(category.IsDefault);
        Assert.Equal(LauncherEntry.DefaultCategory, category.Value);
    }

    [Fact]
    public void FromInput_NonDefaultCategory_PreservesValueAndDisplay()
    {
        var category = Category.FromInput("  Dev  ");

        Assert.False(category.IsDefault);
        Assert.False(category.IsAll);
        Assert.Equal("Dev", category.Value);
        Assert.Equal("Dev", category.ToDisplayLabel());
    }

    [Fact]
    public void IsDefault_AndIsAll_AreExclusive()
    {
        var defaultCategory = Category.Default;
        var allCategory = Category.All;
        var normalCategory = Category.FromInput("Dev");

        Assert.True(defaultCategory.IsDefault && !defaultCategory.IsAll);
        Assert.True(allCategory.IsAll && !allCategory.IsDefault);
        Assert.False(normalCategory.IsDefault || normalCategory.IsAll);
    }
}
