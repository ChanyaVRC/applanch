using applanch.Infrastructure.Localization;
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

    internal static List<Category> BuildCategoryNames(IEnumerable<LaunchItemViewModel> items, CategorySortMode sortMode)
    {
        var categories = CollectCategories(items);

        if (sortMode != CategorySortMode.AsAdded)
        {
            categories.Sort((a, b) => a.ToDisplayLabel().CompareTo(b.ToDisplayLabel()));
        }

        // Ensure Default category is at the end
        categories.Remove(Category.Default);
        categories.Add(Category.Default);

        return categories;
    }

    private static List<Category> CollectCategories(IEnumerable<LaunchItemViewModel> items)
    {
        var seen = new HashSet<string>(StringComparer.Ordinal);
        var categories = new List<Category>();

        foreach (var item in items)
        {
            var category = item.Category;
            if (category.IsDefault || !seen.Add(category.Value))
            {
                continue;
            }

            categories.Add(category);
        }

        return categories;
    }

    private static HashSet<string> BuildKnownAllCategoriesLabels()
    {
        var resourceManager = AppResourceManagerProvider.Instance;
        var labels = new HashSet<string>(StringComparer.Ordinal);

        foreach (var culture in LanguageOption.EnumerateSupportedCultures(includeInvariantCulture: true))
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
