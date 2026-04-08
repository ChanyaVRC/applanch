using System.Diagnostics;
using System.Resources;
using applanch.Infrastructure.Storage;

namespace applanch.ViewModels;

internal static class LaunchCategoryCatalog
{
    private static readonly HashSet<string> KnownAllCategoriesLabels = BuildKnownAllCategoriesLabels();

    internal static string AllCategoriesLabel => AppResources.AllCategories;

    internal static bool IsAllCategoriesLabel(string category)
    {
        return Category.FromInput(category).IsAll || KnownAllCategoriesLabels.Contains(category);
    }

    internal static List<string> BuildCategoryNames(IEnumerable<LaunchItemViewModel> items, CategorySortMode sortMode)
    {
        var categories = CollectCategories(items);

        if (sortMode != CategorySortMode.AsAdded)
        {
            categories.Sort(StringComparer.CurrentCulture);
        }

        var defaultCategoryLabel = Category.Default.ToDisplayLabel();
        Debug.Assert(!string.IsNullOrWhiteSpace(defaultCategoryLabel), "Default category should have a non-empty display label.");

        categories.Remove(defaultCategoryLabel);
        categories.Add(defaultCategoryLabel);

        return categories;
    }

    private static List<string> CollectCategories(IEnumerable<LaunchItemViewModel> items)
    {
        var seen = new HashSet<string>(StringComparer.Ordinal);
        var categories = new List<string>();

        foreach (var item in items)
        {
            var category = item.Category;
            if (category.IsDefault || !seen.Add(category.Value))
            {
                continue;
            }

            categories.Add(category.Value);
        }

        return categories;
    }

    private static HashSet<string> BuildKnownAllCategoriesLabels()
    {
        var resourceManager = new ResourceManager(typeof(AppResources).FullName!, typeof(AppResources).Assembly);
        var labels = new HashSet<string>(StringComparer.Ordinal);

        foreach (var culture in LanguageOptionMap.EnumerateSupportedCultures(includeInvariantCulture: true))
        {
            var value = resourceManager.GetString(nameof(AppResources.AllCategories), culture);
            if (!string.IsNullOrWhiteSpace(value))
            {
                labels.Add(value);
            }
        }

        return labels;
    }
}