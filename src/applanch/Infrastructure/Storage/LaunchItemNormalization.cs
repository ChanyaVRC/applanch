using System.IO;
using System.Resources;

namespace applanch.Infrastructure.Storage;

internal static class LaunchItemNormalization
{
    // Collect localized forms of "DefaultCategory" so persisted labels map to the canonical internal value.
    private static readonly HashSet<string> KnownDefaultCategories = BuildKnownDefaultCategories();

    private static HashSet<string> BuildKnownDefaultCategories()
    {
        var resourceManager = new ResourceManager(typeof(AppResources).FullName!, typeof(AppResources).Assembly);
        var categories = new HashSet<string>(StringComparer.Ordinal);

        foreach (var culture in LanguageOptionMap.EnumerateSupportedCultures(includeInvariantCulture: true))
        {
            var value = resourceManager.GetString(nameof(AppResources.DefaultCategory), culture);
            if (!string.IsNullOrWhiteSpace(value))
            {
                categories.Add(value);
            }
        }

        return categories;
    }

    public static string NormalizeCategory(string? category)
    {
        var trimmed = category?.Trim();
        if (string.IsNullOrEmpty(trimmed))
        {
            return LauncherEntry.DefaultCategory;
        }
        if (KnownDefaultCategories.Contains(trimmed))
        {
            return LauncherEntry.DefaultCategory;
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
