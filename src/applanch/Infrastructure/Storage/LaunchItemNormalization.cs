using System.Globalization;
using System.IO;
using System.Resources;
using applanch.Properties;

namespace applanch.Infrastructure.Storage;

internal static class LaunchItemNormalization
{
    // Collect every localized form of "DefaultCategory" so that a category stored
    // under one language is correctly re-mapped when the app runs under another.
    private static readonly HashSet<string> KnownDefaultCategories =
        BuildKnownDefaultCategories();

    private static HashSet<string> BuildKnownDefaultCategories()
    {
        var rm = new ResourceManager(typeof(Resources).FullName!, typeof(Resources).Assembly);
        var set = new HashSet<string>(StringComparer.Ordinal);
        foreach (var culture in new CultureInfo[] { CultureInfo.InvariantCulture, new("en"), new("ja") })
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
        if (string.IsNullOrWhiteSpace(category))
        {
            return LauncherStore.LauncherEntry.DefaultCategory;
        }

        var trimmed = category.Trim();

        return KnownDefaultCategories.Contains(trimmed)
            ? LauncherStore.LauncherEntry.DefaultCategory
            : trimmed;
    }

    public static string NormalizeArguments(string? arguments) =>
        string.IsNullOrWhiteSpace(arguments) ? string.Empty : arguments.Trim();

    public static string NormalizeDisplayName(string? displayName, string path) =>
        string.IsNullOrWhiteSpace(displayName) ? Path.GetFileNameWithoutExtension(path) : displayName.Trim();
}
