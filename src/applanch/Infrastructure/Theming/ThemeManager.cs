using Microsoft.Win32;
using System.Windows;
using System.Windows.Media;
using applanch.Infrastructure.Storage;

namespace applanch.Infrastructure.Theming;

internal sealed class ThemeManager
{
    private const string PersonalizeRegistryPath = @"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize";
    private const string AppsUseLightTheme = "AppsUseLightTheme";

    private readonly ThemePaletteConfiguration _configuration;
    private readonly Dictionary<string, Dictionary<string, SolidColorBrush>> _brushesByThemeId;
    private readonly Func<AppSettings> _settingsProvider;

    public ThemeManager(
        Func<AppSettings>? settingsProvider = null,
        ThemePaletteConfiguration? configuration = null)
    {
        _settingsProvider = settingsProvider ?? AppSettings.Load;
        _configuration = configuration ?? ThemePaletteConfigurationLoader.LoadForRuntime();
        _brushesByThemeId = BuildBrushMaps(_configuration);
    }

    public void ApplyTheme(ResourceDictionary resources)
    {
        var selectedThemeId = ResolveThemeId(_settingsProvider());
        if (!_brushesByThemeId.TryGetValue(selectedThemeId, out var brushMap))
        {
            brushMap = _brushesByThemeId.Values.First();
        }

        foreach (var entry in _configuration.Entries)
        {
            resources[entry.Key] = brushMap[entry.Key];
        }
    }

    public void ApplyTheme(ResourceDictionary resources, IEnumerable<Window> windows)
    {
        ApplyTheme(resources);

        foreach (var window in windows)
        {
            WindowCaptionThemeHelper.Apply(window);
        }
    }

    private static Dictionary<string, Dictionary<string, SolidColorBrush>> BuildBrushMaps(ThemePaletteConfiguration configuration)
    {
        var brushMaps = new Dictionary<string, Dictionary<string, SolidColorBrush>>(StringComparer.OrdinalIgnoreCase);
        var availableThemeIds = configuration.Themes
            .Select(static x => x.Id)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        foreach (var themeId in availableThemeIds)
        {
            brushMaps[themeId] = new Dictionary<string, SolidColorBrush>(configuration.Entries.Count, StringComparer.Ordinal);
        }

        foreach (var entry in configuration.Entries)
        {
            foreach (var themeId in availableThemeIds)
            {
                var hex = ResolveHex(entry, themeId);
                var color = ColorFromHex(hex);
                var brush = new SolidColorBrush(color);
                brush.Freeze();
                brushMaps[themeId][entry.Key] = brush;
            }
        }

        return brushMaps;
    }

    private static string ResolveHex(ThemePaletteEntry entry, string themeId)
    {
        if (entry.ColorsByThemeId.TryGetValue(themeId, out var hex) && !string.IsNullOrWhiteSpace(hex))
        {
            return hex;
        }

        if (entry.ColorsByThemeId.TryGetValue(ThemePaletteConfigurationLoader.LightThemeId, out var lightHex) && !string.IsNullOrWhiteSpace(lightHex))
        {
            return lightHex;
        }

        return entry.ColorsByThemeId.Values.First(static x => !string.IsNullOrWhiteSpace(x));
    }

    private string ResolveThemeId(AppSettings settings)
    {
        if (string.Equals(settings.ThemeId, ThemePaletteConfigurationLoader.SystemThemeId, StringComparison.OrdinalIgnoreCase))
        {
            var preferredThemeId = ReadWindowsThemePreference()
                ? ThemePaletteConfigurationLoader.LightThemeId
                : ThemePaletteConfigurationLoader.DarkThemeId;
            if (_brushesByThemeId.ContainsKey(preferredThemeId))
            {
                return preferredThemeId;
            }

            return _brushesByThemeId.Keys.First();
        }

        var selectedThemeId = settings.ThemeId.Trim();
        if (_brushesByThemeId.ContainsKey(selectedThemeId))
        {
            return selectedThemeId;
        }

        return _brushesByThemeId.ContainsKey(ThemePaletteConfigurationLoader.LightThemeId)
            ? ThemePaletteConfigurationLoader.LightThemeId
            : _brushesByThemeId.Keys.First();
    }

    private static Color ColorFromHex(string hex)
    {
        return (Color)ColorConverter.ConvertFromString(hex)!;
    }

    private static bool ReadWindowsThemePreference()
    {
        using var key = Registry.CurrentUser.OpenSubKey(PersonalizeRegistryPath);
        var value = key?.GetValue(AppsUseLightTheme);
        return value is not int intValue || intValue != 0;
    }
}

