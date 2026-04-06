using applanch.Infrastructure.Utilities;

namespace applanch.Infrastructure.Launch.AppIdResolvers;

/// <summary>
/// Resolves app IDs by returning a static value.
/// </summary>
internal sealed class StaticAppIdResolver : IAppIdResolver
{
    private readonly string _appId;

    internal StaticAppIdResolver(string appId)
    {
        _appId = appId;
    }

    public bool CanResolve(LaunchPath launchPath)
    {
        return !string.IsNullOrWhiteSpace(_appId);
    }

    public string Resolve(LaunchPath launchPath)
    {
        if (!CanResolve(launchPath))
        {
            throw new AppIdResolutionException("Static app ID is empty.");
        }

        return _appId;
    }
}
