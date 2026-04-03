using System.IO;
using System.Resources;

namespace applanch.Infrastructure.Storage;

internal static class LaunchItemNormalization
{
    // Collect every localized form of "DefaultCategory" so that a category stored
    // under one language is correctly re-mapped when the app runs under another.
    private static readonly HashSet<string> KnownDefaultCategories =
        BuildKnownDefaultCategories();

    private static HashSet<string> BuildKnownDefaultCategories()
    {
        var rm = new ResourceManager(typeof(AppResources).FullName!, typeof(AppResources).Assembly);
        var set = new HashSet<string>(StringComparer.Ordinal);
        foreach (var culture in LanguageOptionMap.EnumerateSupportedCultures(includeInvariantCulture: true))
        {
            var value = rm.GetString("DefaultCategory", culture);
            if (value is not null)
            {
                set.Add(value);
            }
        }

        return set;
    }

    public static string NormalizeCategory(string? category)
    {
        if (category is null)
        {
            return LauncherEntry.DefaultCategory;
        }

        var trimmed = category.AsSpan().Trim();
        if (trimmed.IsEmpty)
        {
            return LauncherEntry.DefaultCategory;
        }

        if (IsKnownDefaultCategory(trimmed))
        {
            return LauncherEntry.DefaultCategory;
        }

        return trimmed.Length == category.Length ? category : trimmed.ToString();
    }

    public static string NormalizeArguments(string? arguments)
    {
        if (arguments is null)
        {
            return string.Empty;
        }

        var trimmed = arguments.AsSpan().Trim();
        if (trimmed.IsEmpty)
        {
            return string.Empty;
        }

        return trimmed.Length == arguments.Length ? arguments : trimmed.ToString();
    }

    public static string NormalizeDisplayName(string? displayName, string path)
    {
        if (displayName is null)
        {
            return GetDisplayNameFromPath(path);
        }

        var trimmed = displayName.AsSpan().Trim();
        if (trimmed.IsEmpty)
        {
            return GetDisplayNameFromPath(path);
        }

        return trimmed.Length == displayName.Length ? displayName : trimmed.ToString();
    }

    private static bool IsKnownDefaultCategory(ReadOnlySpan<char> category)
    {
        foreach (var known in KnownDefaultCategories)
        {
            if (category.Equals(known, StringComparison.Ordinal))
            {
                return true;
            }
        }

        return false;
    }

    private static string GetDisplayNameFromPath(string path) =>
        Path.GetFileNameWithoutExtension(path);
}
