using System.Reflection;
using applanch.Updates;

namespace applanch.Core.Utilities;

/// <summary>
/// Provides the current application version from assembly metadata.
/// </summary>
public static class AppVersionProvider
{
    /// <summary>
    /// Gets the current application version.
    /// </summary>
    public static SemanticVersion CurrentVersion { get; } = GetVersion();

    private static SemanticVersion GetVersion()
    {
        var sourceAssembly = Assembly.GetEntryAssembly() ?? Assembly.GetExecutingAssembly();
        var informational = sourceAssembly
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?
            .InformationalVersion;
        var normalizedInformational = NormalizeInformationalVersion(informational);

        if (!string.IsNullOrEmpty(normalizedInformational) &&
            SemanticVersion.TryParse(normalizedInformational, out var informationalVersion))
        {
            return informationalVersion;
        }

        var assemblyVersion = sourceAssembly.GetName().Version?.ToString() ?? "0.0.0";
        if (SemanticVersion.TryParse(assemblyVersion, out var parsedAssemblyVersion))
        {
            return parsedAssemblyVersion;
        }

        return new SemanticVersion(0, 0, 0, string.Empty);
    }

    private static string? NormalizeInformationalVersion(string? informationalVersion)
    {
        if (string.IsNullOrWhiteSpace(informationalVersion))
        {
            return null;
        }

        var plusIndex = informationalVersion.IndexOf('+');
        return plusIndex >= 0
            ? informationalVersion[..plusIndex]
            : informationalVersion;
    }
}
