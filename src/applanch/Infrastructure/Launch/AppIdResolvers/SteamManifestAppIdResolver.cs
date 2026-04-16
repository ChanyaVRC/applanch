using System.IO;
using applanch.Core.Utilities;

namespace applanch.Infrastructure.Launch.AppIdResolvers;

/// <summary>
/// Resolves app IDs from Steam manifest files.
/// </summary>
[AppIdSource("steam-manifest")]
internal sealed class SteamManifestAppIdResolver : IAppIdResolver
{
    public bool CanResolve(LaunchPath launchPath)
    {
        var launchPathValue = launchPath.Value;

        var steamAppsRoot = FindContainingDirectory(launchPathValue, "steamapps");
        if (steamAppsRoot is null)
        {
            return false;
        }

        return IsUnderSteamCommon(launchPathValue, steamAppsRoot);
    }

    public string Resolve(LaunchPath launchPath)
    {
        var launchPathValue = launchPath.Value;
        var steamAppsRoot = FindContainingDirectory(launchPathValue, "steamapps")
            ?? throw new AppIdResolutionException("The launch path is not inside a Steam library (steamapps not found).");

        var gameDirectory = GetSteamGameDirectory(launchPathValue, steamAppsRoot)
            ?? throw new AppIdResolutionException("The launch path is not under 'steamapps/common'.");

        foreach (var manifestPath in Directory.EnumerateFiles(steamAppsRoot, "appmanifest_*.acf", SearchOption.TopDirectoryOnly))
        {
            var manifest = ReadSteamManifest(manifestPath);
            if (manifest is not null && string.Equals(manifest.Value.InstallDir, gameDirectory, StringComparison.OrdinalIgnoreCase))
            {
                return manifest.Value.AppId;
            }
        }

        throw new AppIdResolutionException($"No Steam manifest matched install directory '{gameDirectory}'.");
    }

    private static bool IsUnderSteamCommon(string launchPath, string steamAppsRoot)
    {
        return GetSteamGameDirectory(launchPath, steamAppsRoot) is not null;
    }

    private static string? GetSteamGameDirectory(string launchPath, string steamAppsRoot)
    {
        var commonRoot = Path.Combine(steamAppsRoot, "common") + Path.DirectorySeparatorChar;
        if (!launchPath.StartsWith(commonRoot, StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        var relativeSpan = launchPath.AsSpan(commonRoot.Length);
        var sepIndex = relativeSpan.IndexOfAny(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        var gameDirectory = (sepIndex >= 0 ? relativeSpan[..sepIndex] : relativeSpan).ToString();
        return string.IsNullOrWhiteSpace(gameDirectory) ? null : gameDirectory;
    }

    private static (string AppId, string InstallDir)? ReadSteamManifest(string manifestPath)
    {
        var appId = string.Empty;
        var installDir = string.Empty;

        foreach (var line in File.ReadLines(manifestPath))
        {
            var trimmed = line.AsSpan().Trim();
            if (trimmed.StartsWith("\"appid\"", StringComparison.OrdinalIgnoreCase))
            {
                appId = ExtractQuotedValue(trimmed);
            }
            else if (trimmed.StartsWith("\"installdir\"", StringComparison.OrdinalIgnoreCase))
            {
                installDir = ExtractQuotedValue(trimmed);
            }
        }

        return !string.IsNullOrWhiteSpace(appId) && !string.IsNullOrWhiteSpace(installDir)
            ? (appId, installDir)
            : null;
    }

    private static string ExtractQuotedValue(ReadOnlySpan<char> line)
    {
        // Splitting `"key"  "value"` by '"' yields ["", "key", "  ", "value", ""].
        // The value token is always at index 3; fewer parts means the line is malformed.
        var span = line;
        Span<Range> parts = stackalloc Range[5];
        return span.Split(parts, '"') >= 4 ? span[parts[3]].ToString() : string.Empty;
    }

    private static string? FindContainingDirectory(string filePath, string targetDirectoryName)
    {
        var current = Path.GetDirectoryName(filePath);
        while (!string.IsNullOrWhiteSpace(current))
        {
            if (string.Equals(Path.GetFileName(current), targetDirectoryName, StringComparison.OrdinalIgnoreCase))
            {
                return current;
            }

            current = Directory.GetParent(current)?.FullName;
        }

        return null;
    }
}