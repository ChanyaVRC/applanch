using System.Diagnostics;
using System.IO;

namespace applanch.Infrastructure.Launch;

internal sealed class LaunchFallbackResolver : ILaunchFallbackResolver
{
    private readonly IReadOnlyList<LaunchFallbackRuleConfiguration> _rules;

    internal LaunchFallbackResolver(LaunchFallbackConfiguration configuration)
    {
        _rules = configuration.Rules;
    }

    internal static LaunchFallbackResolver CreateDefault()
    {
        var configuration = LaunchFallbackConfigurationLoader.Load();
        return new LaunchFallbackResolver(configuration);
    }

    public bool TryCreate(string launchPath, bool runAsAdministrator, out ProcessStartInfo fallback, out string fallbackName)
    {
        foreach (var rule in _rules)
        {
            if (!rule.Enabled)
            {
                continue;
            }

            if (!RuleMatchesPath(rule, launchPath))
            {
                continue;
            }

            if (TryCreateFromRule(rule, launchPath, runAsAdministrator, out fallback))
            {
                fallbackName = rule.Name;
                return true;
            }
        }

        fallback = default!;
        fallbackName = string.Empty;
        return false;
    }

    private static bool RuleMatchesPath(LaunchFallbackRuleConfiguration rule, string launchPath)
    {
        if (rule.MatchFileNames.Count > 0)
        {
            var fileName = Path.GetFileName(launchPath);
            if (!rule.MatchFileNames.Any(name => string.Equals(name, fileName, StringComparison.OrdinalIgnoreCase)))
            {
                return false;
            }
        }

        if (!string.IsNullOrWhiteSpace(rule.PathContains))
        {
            var normalizedNeedle = rule.PathContains.Replace('\\', '/');
            var normalizedPath = launchPath.Replace('\\', '/');
            if (!normalizedPath.Contains(normalizedNeedle, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }
        }

        return true;
    }

    private static bool TryCreateFromRule(
        LaunchFallbackRuleConfiguration rule,
        string launchPath,
        bool runAsAdministrator,
        out ProcessStartInfo fallback)
    {
        fallback = default!;

        switch (rule.Kind.ToLowerInvariant())
        {
            case "riot-client":
                return TryCreateRiotClientFallback(rule, launchPath, runAsAdministrator, out fallback);
            case "steam-rungameid":
                return TryCreateSteamFallback(launchPath, runAsAdministrator, out fallback);
            case "uri-template":
                return TryCreateUriTemplateFallback(rule, runAsAdministrator, out fallback);
            default:
                return false;
        }
    }

    private static bool TryCreateUriTemplateFallback(
        LaunchFallbackRuleConfiguration rule,
        bool runAsAdministrator,
        out ProcessStartInfo fallback)
    {
        fallback = default!;

        if (string.IsNullOrWhiteSpace(rule.UriTemplate) || string.IsNullOrWhiteSpace(rule.AppId))
        {
            return false;
        }

        var launchTarget = rule.UriTemplate.Replace("{appId}", rule.AppId, StringComparison.Ordinal);
        fallback = new ProcessStartInfo
        {
            UseShellExecute = true,
            FileName = launchTarget,
            Arguments = string.Empty,
        };

        if (runAsAdministrator)
        {
            fallback.Verb = "runas";
        }

        return true;
    }

    private static bool TryCreateRiotClientFallback(
        LaunchFallbackRuleConfiguration rule,
        string launchPath,
        bool runAsAdministrator,
        out ProcessStartInfo fallback)
    {
        fallback = default!;

        if (string.IsNullOrWhiteSpace(rule.Product))
        {
            return false;
        }

        if (!TryFindContainingDirectory(launchPath, "Riot Games", out var riotGamesRoot))
        {
            return false;
        }

        var riotClientPath = Path.Combine(riotGamesRoot, "Riot Client", "RiotClientServices.exe");
        if (!File.Exists(riotClientPath))
        {
            return false;
        }

        var patchline = string.IsNullOrWhiteSpace(rule.Patchline) ? "live" : rule.Patchline;
        fallback = new ProcessStartInfo
        {
            UseShellExecute = true,
            FileName = riotClientPath,
            Arguments = $"--launch-product={rule.Product} --launch-patchline={patchline}",
        };

        if (runAsAdministrator)
        {
            fallback.Verb = "runas";
        }

        return true;
    }

    private static bool TryCreateSteamFallback(string launchPath, bool runAsAdministrator, out ProcessStartInfo fallback)
    {
        fallback = default!;

        if (!TryFindContainingDirectory(launchPath, "steamapps", out var steamAppsRoot))
        {
            return false;
        }

        var steamRoot = Directory.GetParent(steamAppsRoot)?.FullName;
        if (string.IsNullOrWhiteSpace(steamRoot))
        {
            return false;
        }

        var steamExe = Path.Combine(steamRoot, "steam.exe");
        if (!File.Exists(steamExe))
        {
            return false;
        }

        if (!TryResolveSteamAppId(launchPath, steamAppsRoot, out var appId))
        {
            return false;
        }

        fallback = new ProcessStartInfo
        {
            UseShellExecute = true,
            FileName = steamExe,
            Arguments = $"steam://rungameid/{appId}",
        };

        if (runAsAdministrator)
        {
            fallback.Verb = "runas";
        }

        return true;
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
