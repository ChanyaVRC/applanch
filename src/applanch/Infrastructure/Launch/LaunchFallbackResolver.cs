using System.Diagnostics;
using System.IO;
using System.Text.RegularExpressions;
using System.Text;

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

    public bool TryCreatePreferred(string launchPath, bool runAsAdministrator, out ProcessStartInfo fallback, out string fallbackName)
    {
        return TryCreateCore(launchPath, runAsAdministrator, "always", out fallback, out fallbackName);
    }

    public bool TryCreate(string launchPath, bool runAsAdministrator, out ProcessStartInfo fallback, out string fallbackName)
    {
        return TryCreateCore(launchPath, runAsAdministrator, "access-denied", out fallback, out fallbackName);
    }

    private bool TryCreateCore(
        string launchPath,
        bool runAsAdministrator,
        string requiredTrigger,
        out ProcessStartInfo fallback,
        out string fallbackName)
    {
        foreach (var rule in _rules)
        {
            if (!rule.Enabled)
            {
                continue;
            }

            if (!string.Equals(NormalizeTrigger(rule.FallbackTrigger), requiredTrigger, StringComparison.OrdinalIgnoreCase))
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
            case "uri-template":
                return TryCreateUriTemplateFallback(rule, launchPath, runAsAdministrator, out fallback);
            case "command-template":
                return TryCreateCommandTemplateFallback(rule, launchPath, runAsAdministrator, out fallback);
            default:
                return false;
        }
    }

    private static bool TryCreateUriTemplateFallback(
        LaunchFallbackRuleConfiguration rule,
        string launchPath,
        bool runAsAdministrator,
        out ProcessStartInfo fallback)
    {
        fallback = default!;

        if (string.IsNullOrWhiteSpace(rule.UriTemplate))
        {
            return false;
        }

        var values = BuildTemplateValues(rule, launchPath);
        if (values is null)
        {
            return false;
        }

        var launchTarget = ExpandTemplate(rule.UriTemplate, values);
        if (string.IsNullOrWhiteSpace(launchTarget))
        {
            return false;
        }

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

    private static bool TryCreateCommandTemplateFallback(
        LaunchFallbackRuleConfiguration rule,
        string launchPath,
        bool runAsAdministrator,
        out ProcessStartInfo fallback)
    {
        fallback = default!;

        if (string.IsNullOrWhiteSpace(rule.FileNameTemplate))
        {
            return false;
        }

        var values = BuildTemplateValues(rule, launchPath);
        if (values is null)
        {
            return false;
        }

        var fileName = ExpandTemplate(rule.FileNameTemplate, values);
        if (string.IsNullOrWhiteSpace(fileName))
        {
            return false;
        }

        if (Path.IsPathFullyQualified(fileName) && !Path.Exists(fileName))
        {
            return false;
        }

        var arguments = ExpandTemplate(rule.ArgumentsTemplate, values);
        fallback = new ProcessStartInfo
        {
            UseShellExecute = true,
            FileName = fileName,
            Arguments = arguments,
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

    private static Dictionary<string, string>? BuildTemplateValues(LaunchFallbackRuleConfiguration rule, string launchPath)
    {
        var appId = ResolveAppId(rule, launchPath);
        if (appId is null)
        {
            return null;
        }

        var launchDirectory = Path.GetDirectoryName(launchPath) ?? string.Empty;
        var launchFileName = Path.GetFileName(launchPath);
        var launchFileStem = Path.GetFileNameWithoutExtension(launchPath);
        return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["appId"] = appId,
            ["product"] = rule.Product,
            ["patchline"] = rule.Patchline,
            ["launchPath"] = launchPath,
            ["launchDirectory"] = launchDirectory,
            ["launchFileName"] = launchFileName,
            ["launchFileStem"] = launchFileStem,
            ["launchPathQuoted"] = Quote(launchPath),
            ["launchDirectoryQuoted"] = Quote(launchDirectory),
        };
    }

    private static string? ResolveAppId(LaunchFallbackRuleConfiguration rule, string launchPath)
    {
        if (!string.IsNullOrWhiteSpace(rule.AppId))
        {
            return rule.AppId;
        }

        return rule.AppIdSource.ToLowerInvariant() switch
        {
            "" => string.Empty,
            "steam-manifest" => TryResolveSteamAppIdFromLaunchPath(launchPath, out var appId) ? appId : null,
            _ => null,
        };
    }

    private static bool TryResolveSteamAppIdFromLaunchPath(string launchPath, out string appId)
    {
        appId = string.Empty;

        if (!TryFindContainingDirectory(launchPath, "steamapps", out var steamAppsRoot))
        {
            return false;
        }

        return TryResolveSteamAppId(launchPath, steamAppsRoot, out appId);
    }

    private static string ExpandTemplate(string template, IReadOnlyDictionary<string, string> values)
    {
        if (string.IsNullOrWhiteSpace(template))
        {
            return string.Empty;
        }

        var builder = new StringBuilder(template);
        foreach (var pair in values)
        {
            builder.Replace($"{{{pair.Key}}}", pair.Value ?? string.Empty);
        }

        var expanded = ExpandAncestorTokens(builder.ToString(), values["launchPath"]);
        return Environment.ExpandEnvironmentVariables(expanded);
    }

    private static string ExpandAncestorTokens(string template, string launchPath)
    {
        return Regex.Replace(
            template,
            "\\{(ancestorPath|ancestorPathQuoted):([^}]+)\\}",
            match =>
            {
                var tokenKind = match.Groups[1].Value;
                var targetDirectoryName = match.Groups[2].Value;
                if (!TryFindContainingDirectory(launchPath, targetDirectoryName, out var directoryPath))
                {
                    return string.Empty;
                }

                return string.Equals(tokenKind, "ancestorPathQuoted", StringComparison.OrdinalIgnoreCase)
                    ? Quote(directoryPath)
                    : directoryPath;
            },
            RegexOptions.IgnoreCase);
    }

    private static string Quote(string value)
    {
        return string.IsNullOrWhiteSpace(value) ? string.Empty : $"\"{value}\"";
    }

    private static string NormalizeTrigger(string trigger)
    {
        return string.IsNullOrWhiteSpace(trigger) ? "access-denied" : trigger.Trim();
    }
}
