using Microsoft.Win32;
using System.Windows;
using applanch.Infrastructure.Storage;

namespace applanch.Infrastructure.Theming;

internal sealed class ThemeManager
{
    private const string PersonalizeRegistryPath = @"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize";
    private const string AppsUseLightTheme = "AppsUseLightTheme";

    private readonly ThemePaletteConfiguration _configuration;
    private readonly Dictionary<string, ThemeDefinition> _themesById;
    private readonly Func<AppSettings> _settingsProvider;

    public ThemeManager(
        Func<AppSettings>? settingsProvider = null,
        ThemePaletteConfiguration? configuration = null)
    {
        _settingsProvider = settingsProvider ?? AppSettings.Load;
        _configuration = configuration ?? ThemePaletteConfigurationLoader.LoadForRuntime();
        _themesById = _configuration.Themes.ToDictionary(static x => x.Id, StringComparer.OrdinalIgnoreCase);
    }

    public void ApplyTheme(ResourceDictionary resources)
    {
        var preferredMode = ReadWindowsThemePreference()
            ? SystemThemeMode.Light
            : SystemThemeMode.Dark;
        var selectedTheme = ResolveTheme(_settingsProvider());
        var brushMap = selectedTheme.CreateBrushMap(_themesById, preferredMode);

        foreach (var (key, brush) in brushMap)
        {
            resources[key] = brush;
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

    private ThemeDefinition ResolveTheme(AppSettings settings)
    {
        var selectedThemeId = settings.ThemeId.Trim();

        if (_themesById.TryGetValue(selectedThemeId, out var selectedTheme))
        {
            return selectedTheme;
        }

        return _themesById.TryGetValue(ThemePaletteConfigurationLoader.LightThemeId, out var lightTheme)
            ? lightTheme
            : _themesById.Values.First();
    }

    private static bool ReadWindowsThemePreference()
    {
        using var key = Registry.CurrentUser.OpenSubKey(PersonalizeRegistryPath);
        var value = key?.GetValue(AppsUseLightTheme);
        return value is not int intValue || intValue != 0;
    }
}

