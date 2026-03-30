using System.IO;

namespace applanch;

internal static class LaunchItemNormalization
{
    public static string NormalizeCategory(string? category) =>
        string.IsNullOrWhiteSpace(category) ? LauncherStore.LauncherEntry.DefaultCategory : category.Trim();

    public static string NormalizeArguments(string? arguments) =>
        string.IsNullOrWhiteSpace(arguments) ? string.Empty : arguments.Trim();

    public static string NormalizeDisplayName(string? displayName, string path) =>
        string.IsNullOrWhiteSpace(displayName) ? Path.GetFileNameWithoutExtension(path) : displayName.Trim();
}