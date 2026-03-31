using System.IO;

namespace applanch.Infrastructure.Launch.AppIdResolvers;

/// <summary>
/// Resolves app IDs from Steam manifest files.
/// </summary>
internal sealed class SteamManifestAppIdResolver : IAppIdResolver
{
    public bool TryResolve(string launchPath, out string appId)
    {
        appId = string.Empty;

        if (!TryFindContainingDirectory(launchPath, "steamapps", out var steamAppsRoot))
        {
            return false;
        }

        return TryResolveSteamAppId(launchPath, steamAppsRoot, out appId);
    }

    private static bool TryResolveSteamAppId(string launchPath, string steamAppsRoot, out string appId)
    {
        appId = string.Empty;

        var commonRoot = Path.Combine(steamAppsRoot, "common") + Path.DirectorySeparatorChar;
        if (!launchPath.StartsWith(commonRoot, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        var relative = launchPath[commonRoot.Length..];
        var gameDirectory = relative.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)[0];
        if (string.IsNullOrWhiteSpace(gameDirectory))
        {
            return false;
        }

        foreach (var manifestPath in Directory.EnumerateFiles(steamAppsRoot, "appmanifest_*.acf", SearchOption.TopDirectoryOnly))
        {
            if (TryReadSteamManifest(manifestPath, out var manifestAppId, out var installDir) &&
                string.Equals(installDir, gameDirectory, StringComparison.OrdinalIgnoreCase))
            {
                appId = manifestAppId;
                return true;
            }
        }

        return false;
    }

    private static bool TryReadSteamManifest(string manifestPath, out string appId, out string installDir)
    {
        appId = string.Empty;
        installDir = string.Empty;

        foreach (var line in File.ReadLines(manifestPath))
        {
            var trimmed = line.Trim();
            if (trimmed.StartsWith("\"appid\"", StringComparison.OrdinalIgnoreCase))
            {
                appId = ExtractQuotedValue(trimmed);
            }
            else if (trimmed.StartsWith("\"installdir\"", StringComparison.OrdinalIgnoreCase))
            {
                installDir = ExtractQuotedValue(trimmed);
            }
        }

        return !string.IsNullOrWhiteSpace(appId) && !string.IsNullOrWhiteSpace(installDir);
    }

    private static string ExtractQuotedValue(string line)
    {
        var parts = line.Split('"', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        return parts.Length >= 2 ? parts[^1] : string.Empty;
    }

    private static bool TryFindContainingDirectory(string filePath, string targetDirectoryName, out string directoryPath)
    {
        directoryPath = string.Empty;
        var current = Path.GetDirectoryName(filePath);
        while (!string.IsNullOrWhiteSpace(current))
        {
            if (string.Equals(Path.GetFileName(current), targetDirectoryName, StringComparison.OrdinalIgnoreCase))
            {
                directoryPath = current;
                return true;
            }

            current = Directory.GetParent(current)?.FullName;
        }

        return false;
    }
}
