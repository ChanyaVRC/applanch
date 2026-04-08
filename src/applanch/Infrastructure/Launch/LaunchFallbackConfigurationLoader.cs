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
        ConfigJsonLoadHelper.LoadAndMerge(
            ConfigJsonPathResolver.EnumerateBundledAndUserDefined(
                appBaseDirectory,
                "launch-fallbacks.json",
                UserDefinedLaunchFallbacksDirectoryName),
            ConfigDescription,
            static path => ConfigJsonLoadHelper.DeserializeFile<LaunchFallbackConfiguration>(path, ConfigJsonLoadHelper.SerializerOptions),
            config => merged.Rules.AddRange(config.Rules));

        return merged;
    }
}
