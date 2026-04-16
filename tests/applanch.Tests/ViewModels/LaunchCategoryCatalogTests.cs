using applanch.Infrastructure.Storage;
using applanch.Settings;
using applanch.ViewModels;
using Xunit;

namespace applanch.Tests.ViewModels;

public class LaunchCategoryCatalogTests
{
    [Fact]
    public void BuildCategoryNames_AsAdded_PreservesFirstAppearanceOrder()
    {
        var items = new[]
        {
            Item("Ops"),
            Item("Dev"),
            Item("Ops"),
            Item("Neko"),
        };

        var categories = LaunchCategoryCatalog.BuildCategoryNames(items, CategorySortMode.AsAdded);

        Assert.Equal(
            new[] { "Ops", "Dev", "Neko", AppResources.DefaultCategory },
            categories.Select(c => c.ToDisplayLabel()));
    }

    [Fact]
    public void BuildCategoryNames_PinsDefaultCategoryLabelLast()
    {
        var items = new[]
        {
            Item(LauncherEntry.DefaultCategory),
            Item("Dev"),
            Item("Ops"),
        };

        var categories = LaunchCategoryCatalog.BuildCategoryNames(items, CategorySortMode.Alphabetical);

        Assert.Equal(AppResources.DefaultCategory, categories.Last().ToDisplayLabel());
    }

    [Fact]
    public void IsAllCategoriesLabel_RecognizesCurrentLocalizedLabel()
    {
        Assert.True(LaunchCategoryCatalog.IsAllCategoriesLabel(AppResources.AllCategories));
    }

    private static LaunchItemViewModel Item(string category)
    {
        return new LaunchItemViewModel(new LaunchPath(@"C:\Tools\App.exe"), Category.FromInput(category), string.Empty, category);
    }
}