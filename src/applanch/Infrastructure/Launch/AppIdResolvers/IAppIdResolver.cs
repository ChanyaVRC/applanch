using applanch.Core.Utilities;

namespace applanch.Infrastructure.Launch.AppIdResolvers;

/// <summary>
/// Resolves application IDs for launch fallback rules.
/// </summary>
internal interface IAppIdResolver
{
    /// <summary>
    /// Checks whether this resolver can resolve for the provided launch path.
    /// </summary>
    bool CanResolve(LaunchPath launchPath);

    /// <summary>
    /// Resolves the application ID.
    /// </summary>
    /// <exception cref="AppIdResolutionException">Thrown when the application ID cannot be resolved.</exception>
    string Resolve(LaunchPath launchPath);
}
