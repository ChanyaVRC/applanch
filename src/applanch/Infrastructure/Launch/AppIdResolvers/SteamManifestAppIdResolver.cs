using System.IO;
using applanch.Infrastructure.Utilities;

namespace applanch.Infrastructure.Launch.AppIdResolvers;

/// <summary>
/// Resolves app IDs from Steam manifest files.
/// </summary>
internal sealed class SteamManifestAppIdResolver : IAppIdResolver
{
    public bool TryResolve(LaunchPath launchPath, out string appId)
    {
        appId = string.Empty;
        var launchPathValue = launchPath.Value;

        if (!TryFindContainingDirectory(launchPathValue, "steamapps", out var steamAppsRoot))
        {
            return false;
        }

        return TryResolveSteamAppId(launchPathValue, steamAppsRoot, out appId);
    }

    private static bool TryResolveSteamAppId(string launchPath, string steamAppsRoot, out string appId)
    {
        appId = string.Empty;

        var commonRoot = Path.Combine(steamAppsRoot, "common") + Path.DirectorySeparatorChar;
        if (!launchPath.StartsWith(commonRoot, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        var relativeSpan = launchPath.AsSpan(commonRoot.Length);
        var sepIndex = relativeSpan.IndexOfAny(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        var gameDirectory = (sepIndex >= 0 ? relativeSpan[..sepIndex] : relativeSpan).ToString();
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
        var remaining = line.AsSpan();

        // Skip key token and then read value token.
        if (!TryReadNextQuotedToken(ref remaining, out _)
            || !TryReadNextQuotedToken(ref remaining, out var valueToken))
        {
            return string.Empty;
        }

        return valueToken.ToString();
    }

    private static bool TryReadNextQuotedToken(ref ReadOnlySpan<char> text, out ReadOnlySpan<char> token)
    {
        token = default;

        var openQuoteIndex = text.IndexOf('"');
        if (openQuoteIndex < 0)
        {
            return false;
        }

        var afterOpenQuote = text[(openQuoteIndex + 1)..];
        var closeQuoteIndex = afterOpenQuote.IndexOf('"');
        if (closeQuoteIndex < 0)
        {
            return false;
        }

        token = afterOpenQuote[..closeQuoteIndex];
        text = afterOpenQuote[(closeQuoteIndex + 1)..];
        return true;
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
