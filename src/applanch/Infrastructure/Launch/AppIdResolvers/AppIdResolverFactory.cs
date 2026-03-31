namespace applanch.Infrastructure.Launch.AppIdResolvers;

/// <summary>
/// Factory for creating IAppIdResolver instances based on source configuration.
/// </summary>
internal static class AppIdResolverFactory
{
    /// <summary>
    /// Creates a resolver for the given source.
    /// Supported formats:
    /// - "static:VALUE" - Static app ID value
    /// - "steam-manifest" - Resolve from Steam manifest files
    /// - "registry:HIVE:KeyPath:ValueName" - Resolve from Windows Registry
    /// </summary>
    internal static IAppIdResolver? CreateResolver(string? source)
    {
        if (string.IsNullOrWhiteSpace(source))
        {
            return null;
        }

        var trimmedSource = source.Trim();

        // Check for static: prefix
        if (trimmedSource.StartsWith("static:", StringComparison.OrdinalIgnoreCase))
        {
            var value = trimmedSource["static:".Length..];
            return new StaticAppIdResolver(value);
        }

        // Steam manifest (legacy support without prefix)
        if (string.Equals(trimmedSource, "steam-manifest", StringComparison.OrdinalIgnoreCase))
        {
            return new SteamManifestAppIdResolver();
        }

        // Check for registry: prefix
        if (trimmedSource.StartsWith("registry:", StringComparison.OrdinalIgnoreCase))
        {
            return new RegistryAppIdResolver(trimmedSource);
        }

        return null;
    }
}
