using System.IO;

namespace applanch.Infrastructure.Utilities;

internal static class ConfigJsonPathResolver
{
    private const string ConfigDirectoryName = "Config";
    private const string UserDefinedDirectoryName = "UserDefined";

    internal static string GetBundledPath(string appBaseDirectory, string bundledFileName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(appBaseDirectory);
        ArgumentException.ThrowIfNullOrWhiteSpace(bundledFileName);

        return Path.Combine(appBaseDirectory, ConfigDirectoryName, bundledFileName);
    }

    internal static string GetUserDefinedDirectory(string appBaseDirectory, string userDefinedSubDirectoryName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(appBaseDirectory);
        ArgumentException.ThrowIfNullOrWhiteSpace(userDefinedSubDirectoryName);

        return Path.Combine(appBaseDirectory, ConfigDirectoryName, UserDefinedDirectoryName, userDefinedSubDirectoryName);
    }

    internal static IEnumerable<string> EnumerateUserDefinedJsonPaths(string appBaseDirectory, string userDefinedSubDirectoryName)
    {
        var userDefinedDirectory = GetUserDefinedDirectory(appBaseDirectory, userDefinedSubDirectoryName);
        if (!Directory.Exists(userDefinedDirectory))
        {
            return [];
        }

        return Directory
            .EnumerateFiles(userDefinedDirectory, "*.json", SearchOption.TopDirectoryOnly)
            .OrderBy(static path => path, StringComparer.OrdinalIgnoreCase);
    }

    internal static IEnumerable<ConfigJsonPathCandidate> EnumerateBundledAndUserDefined(
        string appBaseDirectory,
        string bundledFileName,
        string userDefinedSubDirectoryName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(appBaseDirectory);
        ArgumentException.ThrowIfNullOrWhiteSpace(bundledFileName);
        ArgumentException.ThrowIfNullOrWhiteSpace(userDefinedSubDirectoryName);

        return EnumerateUserDefinedJsonPaths(appBaseDirectory, userDefinedSubDirectoryName)
            .Select(static path => new ConfigJsonPathCandidate(path, IsBundled: false))
            .Prepend(new ConfigJsonPathCandidate(GetBundledPath(appBaseDirectory, bundledFileName), IsBundled: true));
    }
}
