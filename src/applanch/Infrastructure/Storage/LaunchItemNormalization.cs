using System.IO;
using applanch.Core.Localization;

namespace applanch.Infrastructure.Storage;

internal static class LaunchItemNormalization
{
    // Collect localized forms of "DefaultCategory" so persisted labels map to the canonical internal value.
    private static readonly HashSet<string> KnownDefaultCategoriesLabels = BuildKnownLabels(nameof(AppResources.DefaultCategory));
    private static readonly HashSet<string> KnownAllCategoriesLabels = BuildKnownLabels(nameof(AppResources.AllCategories));

    private static HashSet<string> BuildKnownLabels(string resourceName)
    {
        var resourceManager = AppResources.ResourceManager;
        var labels = new HashSet<string>(StringComparer.Ordinal);

        foreach (var culture in LanguageOption.EnumerateSupportedCultures(includeInvariantCulture: true))
        {
            var value = resourceManager.GetString(resourceName, culture);
            if (!string.IsNullOrWhiteSpace(value))
            {
                labels.Add(value);
            }
        }

        return labels;
    }

    public static string NormalizeCategory(string? category)
    {
        var trimmed = category?.Trim();
        if (string.IsNullOrEmpty(trimmed))
        {
            return LauncherEntry.DefaultCategory;
        }
        if (KnownDefaultCategoriesLabels.Contains(trimmed))
        {
            return LauncherEntry.DefaultCategory;
        }
        if (KnownAllCategoriesLabels.Contains(trimmed))
        {
            return LauncherEntry.AllCategories;
        }

        return trimmed;
    }

    public static string NormalizeArguments(string? arguments)
    {
        var trimmed = arguments?.Trim();
        if (string.IsNullOrEmpty(trimmed))
        {
            return string.Empty;
        }

        return trimmed;
    }

    public static string NormalizeDisplayName(string? displayName, string path)
    {
        var trimmed = displayName?.Trim();
        if (string.IsNullOrEmpty(trimmed))
        {
            return GetDisplayNameFromPath(path);
        }

        return trimmed;
    }

    private static string GetDisplayNameFromPath(string path) =>
        Path.GetFileNameWithoutExtension(path);
}
