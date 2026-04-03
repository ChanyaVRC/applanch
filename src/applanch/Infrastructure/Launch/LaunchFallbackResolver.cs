using System.Diagnostics;
using System.IO;
using System.Text.RegularExpressions;
using System.Text;
using applanch.Infrastructure.Launch.AppIdResolvers;
using applanch.Infrastructure.Utilities;

namespace applanch.Infrastructure.Launch;

internal sealed class LaunchFallbackResolver(LaunchFallbackConfiguration configuration) : ILaunchFallbackResolver
{
    private readonly IReadOnlyList<LaunchFallbackRuleConfiguration> _rules = configuration.Rules;

    internal static LaunchFallbackResolver CreateDefault()
    {
        var configuration = LaunchFallbackConfigurationLoader.Load();
        return new LaunchFallbackResolver(configuration);
    }

    public bool TryCreatePreferred(LaunchPath launchPath, bool runAsAdministrator, out ProcessStartInfo fallback, out string fallbackName)
    {
        return TryCreateCore(launchPath, runAsAdministrator, "always", out fallback, out fallbackName);
    }

    public bool TryCreate(LaunchPath launchPath, bool runAsAdministrator, out ProcessStartInfo fallback, out string fallbackName)
    {
        return TryCreateCore(launchPath, runAsAdministrator, "access-denied", out fallback, out fallbackName);
    }

    private bool TryCreateCore(
        LaunchPath launchPath,
        bool runAsAdministrator,
        string requiredTrigger,
        out ProcessStartInfo fallback,
        out string fallbackName)
    {
        var launchPathValue = launchPath.Value;

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

            if (!RuleMatchesPath(rule, launchPathValue))
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
            if (!rule.MatchFileNames.Contains(fileName, StringComparer.OrdinalIgnoreCase))
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
        LaunchPath launchPath,
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
        LaunchPath launchPath,
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
        LaunchPath launchPath,
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

    private static Dictionary<string, string>? BuildTemplateValues(LaunchFallbackRuleConfiguration rule, LaunchPath launchPath)
    {
        var launchPathValue = launchPath.Value;
        var appId = ResolveAppId(rule, launchPath);
        if (appId is null)
        {
            return null;
        }

        var launchDirectory = Path.GetDirectoryName(launchPathValue) ?? string.Empty;
        var launchFileName = Path.GetFileName(launchPathValue);
        var launchFileStem = Path.GetFileNameWithoutExtension(launchPathValue);
        return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["appId"] = appId,
            ["product"] = rule.Product,
            ["patchline"] = rule.Patchline,
            ["launchPath"] = launchPathValue,
            ["launchDirectory"] = launchDirectory,
            ["launchFileName"] = launchFileName,
            ["launchFileStem"] = launchFileStem,
            ["launchPathQuoted"] = Quote(launchPathValue),
            ["launchDirectoryQuoted"] = Quote(launchDirectory),
        };
    }

    private static string? ResolveAppId(LaunchFallbackRuleConfiguration rule, LaunchPath launchPath)
    {
        // Try static appId first
        if (!string.IsNullOrWhiteSpace(rule.AppId))
        {
            return rule.AppId;
        }

        // Try AppIdSource
        if (string.IsNullOrWhiteSpace(rule.AppIdSource))
        {
            return string.Empty;
        }

        var resolver = AppIdResolverFactory.CreateResolver(rule.AppIdSource);
        if (resolver is null)
        {
            return null;
        }

        return resolver.TryResolve(launchPath, out var appId) ? appId : null;
    }

    private static string ExpandTemplate(string template, Dictionary<string, string> values)
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
