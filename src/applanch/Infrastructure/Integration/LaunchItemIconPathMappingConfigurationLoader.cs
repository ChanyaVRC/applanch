using applanch.Infrastructure.Utilities;

namespace applanch.Infrastructure.Integration;

internal static class LaunchItemIconPathMappingConfigurationLoader
{
    private const string UserDefinedIconPathMappingsDirectoryName = "icon-path-mappings";
    private const string ConfigDescription = "icon path mapping config";

    internal static LaunchItemIconPathMappingConfiguration Load()
    {
        return LoadFromDirectory(AppContext.BaseDirectory);
    }

    internal static LaunchItemIconPathMappingConfiguration LoadFromDirectory(string appBaseDirectory)
    {
        var merged = new LaunchItemIconPathMappingConfiguration { Rules = [] };
        ConfigJsonLoadHelper.LoadAndMerge(
            ConfigJsonPathResolver.EnumerateBundledAndUserDefined(
                appBaseDirectory,
                "icon-path-mappings.json",
                UserDefinedIconPathMappingsDirectoryName),
            ConfigDescription,
            static path => ConfigJsonLoadHelper.DeserializeFile<LaunchItemIconPathMappingConfiguration>(path, ConfigJsonLoadHelper.SerializerOptions),
            config => merged.Rules.AddRange(config.Rules));

        return merged;
    }
}
