using System.IO;
using applanch.Infrastructure.Utilities;

namespace applanch.Infrastructure.Integration;

internal sealed class LaunchItemIconPathResolver(LaunchItemIconPathMappingConfiguration configuration)
{
    private static readonly Lazy<LaunchItemIconPathResolver> RuntimeResolver =
        new(static () => new LaunchItemIconPathResolver(LaunchItemIconPathMappingConfigurationLoader.Load()));

    private readonly IReadOnlyList<LaunchItemIconPathMappingRuleConfiguration> _rules = configuration.Rules;

    internal static string ResolveForRuntime(string launchPath)
    {
        return RuntimeResolver.Value.Resolve(launchPath);
    }

    internal string Resolve(string launchPath)
    {
        if (string.IsNullOrWhiteSpace(launchPath))
        {
            return launchPath;
        }

        var fileName = Path.GetFileName(launchPath);
        var parentDirectoryName = Path.GetFileName(Path.GetDirectoryName(launchPath));

        foreach (var rule in _rules)
        {
            if (!rule.Enabled)
            {
                continue;
            }

            if (rule.MatchFileNames.Count > 0 &&
                !rule.MatchFileNames.Contains(fileName, StringComparer.OrdinalIgnoreCase))
            {
                continue;
            }

            if (!string.IsNullOrWhiteSpace(rule.ParentDirectoryName) &&
                !string.Equals(parentDirectoryName, rule.ParentDirectoryName, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            if (!string.IsNullOrWhiteSpace(rule.PathContains))
            {
                var normalizedPath = launchPath.Replace('\\', '/');
                var normalizedNeedle = rule.PathContains.Replace('\\', '/');
                if (!normalizedPath.Contains(normalizedNeedle, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }
            }

            return ResolveTemplate(rule, launchPath);
        }

        return launchPath;
    }

    private static string ResolveTemplate(LaunchItemIconPathMappingRuleConfiguration rule, string launchPath)
    {
        if (string.IsNullOrWhiteSpace(rule.IconPathTemplate))
        {
            throw new JsonPathResolutionException($"IconPathTemplate is empty for rule '{rule.Name}'.");
        }

        try
        {
            return JsonPathTemplateResolver.ResolveExistingFilePath(rule.IconPathTemplate, launchPath);
        }
        catch (JsonPathResolutionException ex)
        {
            throw new JsonPathResolutionException(
                $"Rule '{rule.Name}' has invalid icon path template '{rule.IconPathTemplate}': {ex.Message}",
                ex);
        }
    }
}