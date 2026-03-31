namespace applanch.Infrastructure.Launch.AppIdResolvers;

/// <summary>
/// Resolves application IDs for launch fallback rules.
/// </summary>
internal interface IAppIdResolver
{
    /// <summary>
    /// Attempts to resolve the application ID.
    /// </summary>
    /// <param name="launchPath">The path to the launched executable.</param>
    /// <param name="appId">The resolved application ID when successful.</param>
    /// <returns>True if resolution succeeded; otherwise, false.</returns>
    bool TryResolve(string launchPath, out string appId);
}
