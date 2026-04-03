using System.Resources;
using applanch.Infrastructure.Storage;

namespace applanch.ViewModels;

internal static class LaunchCategoryCatalog
{
    private static readonly HashSet<string> KnownAllCategoriesLabels = BuildKnownAllCategoriesLabels();

    internal static string AllCategoriesLabel => AppResources.AllCategories;

    internal static bool IsAllCategoriesLabel(string category)
    {
        return KnownAllCategoriesLabels.Contains(category);
    }

    internal static List<string> BuildCategoryNames(IEnumerable<LaunchItemViewModel> items, CategorySortMode sortMode)
    {
        var categories = CollectDistinctNonEmptyCategories(items);

        if (sortMode != CategorySortMode.AsAdded)
        {
            categories.Sort(StringComparer.CurrentCulture);
        }

        var defaultCategory = LauncherEntry.DefaultCategory;
        if (categories.Remove(defaultCategory))
        {
            categories.Add(defaultCategory);
        }

        return categories;
    }

    private static List<string> CollectDistinctNonEmptyCategories(IEnumerable<LaunchItemViewModel> items)
    {
        var seen = new HashSet<string>(StringComparer.Ordinal);
        var categories = new List<string>();

        foreach (var item in items)
        {
            if (string.IsNullOrWhiteSpace(item.Category) || !seen.Add(item.Category))
            {
                continue;
            }

            categories.Add(item.Category);
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