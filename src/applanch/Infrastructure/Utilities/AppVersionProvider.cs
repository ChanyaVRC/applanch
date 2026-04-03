using System.Reflection;

namespace applanch.Infrastructure.Utilities;

internal static class AppVersionProvider
{
    public static string GetDisplayVersion()
    {
        var sourceAssembly = Assembly.GetEntryAssembly() ?? Assembly.GetExecutingAssembly();
        var informational = sourceAssembly
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?
            .InformationalVersion;
        var normalizedInformational = NormalizeInformationalVersion(informational);

        if (!string.IsNullOrEmpty(normalizedInformational))
        {
            return normalizedInformational;
        }

        return sourceAssembly.GetName().Version?.ToString() ?? "0.0.0";
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
