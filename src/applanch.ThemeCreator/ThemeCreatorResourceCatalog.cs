using System.Globalization;

namespace applanch.ThemeCreator;

internal static class ThemeCreatorResourceCatalog
{
    internal static string GetBrushDescription(string key)
    {
        var resourceKey = "BrushDesc_" + key.Replace("Brush.", string.Empty, StringComparison.Ordinal);
        return AppResources.ResourceManager.GetString(resourceKey, CultureInfo.CurrentUICulture)
            ?? AppResources.BrushDesc_Custom;
    }
}
