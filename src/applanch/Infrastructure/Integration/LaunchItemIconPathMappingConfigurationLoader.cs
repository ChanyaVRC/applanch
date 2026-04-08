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
        var loadedAny = false;

        foreach (var candidate in ConfigJsonPathResolver.EnumerateBundledAndUserDefined(
                     appBaseDirectory,
                     "icon-path-mappings.json",
                     UserDefinedIconPathMappingsDirectoryName))
        {
            try
            {
                var config = ConfigJsonLoadHelper.Load<LaunchItemIconPathMappingConfiguration>(
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
            AppLogger.Instance.Info("Icon path mapping config not found.");
        }

        return merged;
    }
}
