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

    public bool TryResolve(LaunchPath launchPath, out string appId)
    {
        if (string.IsNullOrWhiteSpace(_appId))
        {
            appId = string.Empty;
            return false;
        }

        appId = _appId;
        return true;
    }
}
