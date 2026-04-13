using System.Windows.Media;

namespace applanch.Infrastructure.Theming;

internal abstract class ThemeDefinition
{
    protected ThemeDefinition(
        string id,
        LocalizedText displayName,
        bool isVisibleInThemeList = true)
    {
        Id = id.ToLowerInvariant();
        DisplayName = displayName;
        IsVisibleInThemeList = isVisibleInThemeList;
    }

    internal string Id { get; }

    internal LocalizedText DisplayName { get; }

    internal bool IsVisibleInThemeList { get; }

    internal abstract IReadOnlyDictionary<string, string> ColorsByKey { get; }

    internal Dictionary<string, SolidColorBrush> CreateBrushMap(
        IReadOnlyDictionary<string, ThemeDefinition> themesById,
        SystemThemeMode preferredSystemMode)
    {
        var allKeys = new HashSet<string>(StringComparer.Ordinal);
        foreach (var (_, theme) in themesById)
        {
            foreach (var (key, _) in theme.ColorsByKey)
            {
                allKeys.Add(key);
            }
        }

        var brushMap = new Dictionary<string, SolidColorBrush>(allKeys.Count);

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

        foreach (var (_, theme) in themesById)
        {
            if (theme.ColorsByKey.TryGetValue(key, out var candidateHex) &&
                !string.IsNullOrWhiteSpace(candidateHex))
            {
                return candidateHex;
            }
        }

        throw new InvalidOperationException($"No color value found for key '{key}'.");
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
