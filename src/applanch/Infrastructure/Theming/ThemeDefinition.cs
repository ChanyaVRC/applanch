using System.Windows.Media;

namespace applanch.Infrastructure.Theming;

internal abstract class ThemeDefinition
{
    protected ThemeDefinition(
        string id,
        LocalizedText displayName)
    {
        Id = id.ToLowerInvariant();
        DisplayName = displayName;
    }

    internal string Id { get; }

    internal LocalizedText DisplayName { get; }

    internal abstract IReadOnlyDictionary<string, string> ColorsByKey { get; }

    internal Dictionary<string, SolidColorBrush> CreateBrushMap(
        IReadOnlyDictionary<string, ThemeDefinition> themesById,
        SystemThemeMode preferredSystemMode)
    {
        var allKeys = themesById.Values
            .SelectMany(static theme => theme.ColorsByKey.Keys)
            .Distinct()
            .ToArray();
        var brushMap = new Dictionary<string, SolidColorBrush>(allKeys.Length);

        foreach (var key in allKeys)
        {
            var hex = ResolveHex(key, themesById, preferredSystemMode);
            var brush = new SolidColorBrush((Color)ColorConverter.ConvertFromString(hex)!);
            brush.Freeze();
            brushMap[key] = brush;
        }

        return brushMap;
    }

    protected abstract IEnumerable<string> GetRelatedThemeIds(SystemThemeMode preferredSystemMode);

    private string ResolveHex(
        string key,
        IReadOnlyDictionary<string, ThemeDefinition> themesById,
        SystemThemeMode preferredSystemMode)
    {
        var visited = new HashSet<string>();

        if (TryResolveHexInGraph(key, themesById, preferredSystemMode, visited, out var hex))
        {
            return hex;
        }

        if (Id != ThemePaletteConfigurationLoader.LightThemeId &&
            themesById.TryGetValue(ThemePaletteConfigurationLoader.LightThemeId, out var lightTheme) &&
            lightTheme.TryResolveHexInGraph(key, themesById, preferredSystemMode, visited, out var lightHex))
        {
            return lightHex;
        }

        return themesById.Values
            .Select(theme => theme.ColorsByKey.TryGetValue(key, out var candidateHex) ? candidateHex : null)
            .First(static candidateHex => !string.IsNullOrWhiteSpace(candidateHex))!;
    }

    private bool TryResolveHexInGraph(
        string key,
        IReadOnlyDictionary<string, ThemeDefinition> themesById,
        SystemThemeMode preferredSystemMode,
        HashSet<string> visited,
        out string hex)
    {
        if (!visited.Add(Id))
        {
            hex = string.Empty;
            return false;
        }

        try
        {
            if (ColorsByKey.TryGetValue(key, out hex!))
            {
                return true;
            }

            foreach (var relatedId in GetRelatedThemeIds(preferredSystemMode))
            {
                if (themesById.TryGetValue(relatedId, out var relatedTheme) &&
                    relatedTheme.TryResolveHexInGraph(key, themesById, preferredSystemMode, visited, out hex!))
                {
                    return true;
                }
            }

            hex = string.Empty;
            return false;
        }
        finally
        {
            visited.Remove(Id);
        }
    }
}
