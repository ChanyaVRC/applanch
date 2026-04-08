using applanch.Infrastructure.Utilities;

namespace applanch.Infrastructure.Launch;

internal static class LaunchFallbackConfigurationLoader
{
    private const string UserDefinedLaunchFallbacksDirectoryName = "launch-fallbacks";
    private const string ConfigDescription = "launch fallback config";

    internal static LaunchFallbackConfiguration Load()
    {
        return LoadFromDirectory(AppContext.BaseDirectory);
    }

    internal static LaunchFallbackConfiguration LoadFromDirectory(string appBaseDirectory)
    {
        var merged = new LaunchFallbackConfiguration { Rules = [] };
        var loadedAny = false;

        foreach (var candidate in ConfigJsonPathResolver.EnumerateBundledAndUserDefined(
                     appBaseDirectory,
                     "launch-fallbacks.json",
                     UserDefinedLaunchFallbacksDirectoryName))
        {
            try
            {
                var config = ConfigJsonLoadHelper.Load<LaunchFallbackConfiguration>(
                    candidate,
                    ConfigDescription);

                merged.Rules.AddRange(config.Rules);
                loadedAny = true;
            }
            catch (Exception)
            {
            }
        }

        if (!loadedAny)
        {
            AppLogger.Instance.Info("Launch fallback config not found.");
        }

        return merged;
    }
}
