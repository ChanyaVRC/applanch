using System.IO;

namespace applanch.ThemeCreator;

internal static class ThemeCreatorPathResolver
{
    internal static string ResolveDefaultSourcePath()
    {
        var candidates = new[]
        {
            Path.Combine(Environment.CurrentDirectory, "src", "applanch", "Config", "theme-palette.json"),
            Path.Combine(AppContext.BaseDirectory, "Config", "theme-palette.json")
        };

        return candidates.FirstOrDefault(File.Exists)
            ?? candidates[0];
    }

    internal static string ResolveDefaultOutputPath(string sourcePalettePath, string themeId)
    {
        var fileName = SanitizeThemeId(themeId);

        if (TryResolveConfigDirectory(sourcePalettePath, out var configDirectory))
        {
            return Path.Combine(configDirectory, "UserDefined", "theme-palette", fileName + ".json");
        }

        var directory = Path.GetDirectoryName(sourcePalettePath);
        return Path.Combine(
            string.IsNullOrWhiteSpace(directory) ? Environment.CurrentDirectory : directory,
            fileName + ".json");
    }

    private static bool TryResolveConfigDirectory(string sourcePalettePath, out string configDirectory)
    {
        configDirectory = string.Empty;

        if (string.IsNullOrWhiteSpace(sourcePalettePath))
        {
            return false;
        }

        var sourceDirectory = Path.GetDirectoryName(sourcePalettePath);
        if (string.IsNullOrWhiteSpace(sourceDirectory))
        {
            return false;
        }

        var directoryInfo = new DirectoryInfo(sourceDirectory);
        if (string.Equals(directoryInfo.Name, "Config", StringComparison.OrdinalIgnoreCase))
        {
            configDirectory = directoryInfo.FullName;
            return true;
        }

        if (string.Equals(directoryInfo.Name, "theme-palette", StringComparison.OrdinalIgnoreCase) &&
            string.Equals(directoryInfo.Parent?.Name, "UserDefined", StringComparison.OrdinalIgnoreCase) &&
            string.Equals(directoryInfo.Parent?.Parent?.Name, "Config", StringComparison.OrdinalIgnoreCase))
        {
            configDirectory = directoryInfo.Parent!.Parent!.FullName;
            return true;
        }

        return false;
    }

    private static string SanitizeThemeId(string themeId)
    {
        var effectiveThemeId = string.IsNullOrWhiteSpace(themeId)
            ? "my-theme"
            : themeId.Trim();

        foreach (var invalidChar in Path.GetInvalidFileNameChars())
        {
            effectiveThemeId = effectiveThemeId.Replace(invalidChar, '-');
        }

        return effectiveThemeId;
    }
}
