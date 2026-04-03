using applanch.Infrastructure.Storage;
using applanch.Infrastructure.Utilities;
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

        Assert.Equal(["Ops", "Dev", "Neko"], categories);
    }

    [Fact]
    public void BuildCategoryNames_PinsDefaultCategoryLast()
    {
        var items = new[]
        {
            Item(LauncherEntry.DefaultCategory),
            Item("Dev"),
            Item("Ops"),
        };

        var categories = LaunchCategoryCatalog.BuildCategoryNames(items, CategorySortMode.Alphabetical);

        Assert.Equal(LauncherEntry.DefaultCategory, categories.Last());
    }

    [Fact]
    public void IsAllCategoriesLabel_RecognizesCurrentLocalizedLabel()
    {
        Assert.True(LaunchCategoryCatalog.IsAllCategoriesLabel(AppResources.AllCategories));
    }

    private static LaunchItemViewModel Item(string category)
    {
        return new LaunchItemViewModel(new LaunchPath(@"C:\\Tools\\App.exe"), category, string.Empty, category);
    }
}