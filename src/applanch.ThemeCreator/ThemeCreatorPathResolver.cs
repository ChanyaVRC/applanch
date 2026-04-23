using System.IO;

namespace applanch.ThemeCreator;

internal static class ThemeCreatorPathResolver
{
    private const string SourceProjectDirectoryName = "src";
    private const string ApplanchDirectoryName = "applanch";
    private const string ConfigDirectoryName = "Config";
    private const string UserDefinedDirectoryName = "UserDefined";
    private const string ThemePaletteDirectoryName = "theme-palette";
    private const string ThemePaletteFileName = "theme-palette.json";
    private const string JsonFileExtension = ".json";
    private const string DefaultThemeFileNameStem = "my-theme";

    internal static string ResolveDefaultSourcePath()
    {
        var workspaceCandidate = Path.Combine(
            Environment.CurrentDirectory,
            SourceProjectDirectoryName,
            ApplanchDirectoryName,
            ConfigDirectoryName,
            ThemePaletteFileName);

        var appCandidate = Path.Combine(
            AppContext.BaseDirectory,
            ConfigDirectoryName,
            ThemePaletteFileName);

        return File.Exists(workspaceCandidate)
            ? workspaceCandidate
            : appCandidate;
    }

    internal static string ResolveDefaultOutputPath(string sourcePalettePath, string themeId)
    {
        var fileName = SanitizeThemeId(themeId) + JsonFileExtension;

        if (TryResolveConfigDirectory(sourcePalettePath, out var configDirectory))
        {
            return Path.Combine(
                configDirectory,
                UserDefinedDirectoryName,
                ThemePaletteDirectoryName,
                fileName);
        }

        var directory = Path.GetDirectoryName(sourcePalettePath);
        return Path.Combine(
            string.IsNullOrWhiteSpace(directory) ? Environment.CurrentDirectory : directory,
            fileName);
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
        if (string.Equals(directoryInfo.Name, ConfigDirectoryName, StringComparison.OrdinalIgnoreCase))
        {
            configDirectory = directoryInfo.FullName;
            return true;
        }

        if (string.Equals(directoryInfo.Name, ThemePaletteDirectoryName, StringComparison.OrdinalIgnoreCase) &&
            string.Equals(directoryInfo.Parent?.Name, UserDefinedDirectoryName, StringComparison.OrdinalIgnoreCase) &&
            string.Equals(directoryInfo.Parent?.Parent?.Name, ConfigDirectoryName, StringComparison.OrdinalIgnoreCase))
        {
            configDirectory = directoryInfo.Parent!.Parent!.FullName;
            return true;
        }

        return false;
    }

    private static string SanitizeThemeId(string themeId)
    {
        var effectiveThemeId = string.IsNullOrWhiteSpace(themeId)
            ? DefaultThemeFileNameStem
            : themeId.Trim();

        foreach (var invalidChar in Path.GetInvalidFileNameChars())
        {
            effectiveThemeId = effectiveThemeId.Replace(invalidChar, '-');
        }

        effectiveThemeId = effectiveThemeId.Trim(' ', '.');

        return string.IsNullOrWhiteSpace(effectiveThemeId)
            ? DefaultThemeFileNameStem
            : effectiveThemeId;
    }
}
