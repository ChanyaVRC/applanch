using Microsoft.Win32;
using System.Windows;
using applanch.Infrastructure.Storage;

namespace applanch.Infrastructure.Theming;

internal sealed class ThemeApplier
{
    private const string PersonalizeRegistryPath = @"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize";
    private const string AppsUseLightTheme = "AppsUseLightTheme";

    private readonly ThemePaletteConfiguration _configuration;
    private readonly Dictionary<string, ThemeDefinition> _themesById;
    private readonly Func<AppSettings> _settingsProvider;

    public ThemeApplier()
        : this(AppSettings.Load, ThemePaletteConfigurationLoader.LoadForRuntime())
    {
    }

    internal ThemeApplier(
        Func<AppSettings>? settingsProvider = null,
        ThemePaletteConfiguration? configuration = null)
    {
        _settingsProvider = settingsProvider ?? AppSettings.Load;
        _configuration = configuration ?? ThemePaletteConfigurationLoader.LoadForRuntime();
        _themesById = _configuration.Themes.ToDictionary(static x => x.Id);
    }

    public void ApplyTheme(ResourceDictionary resources)
    {
        var preferredMode = ReadWindowsThemePreference();
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
            WindowIconThemeHelper.Apply(window, resources);
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

    private static SystemThemeMode ReadWindowsThemePreference()
    {
        using var key = Registry.CurrentUser.OpenSubKey(PersonalizeRegistryPath);
        var value = key?.GetValue(AppsUseLightTheme);
        return value is int intValue && intValue == 0 ? SystemThemeMode.Dark : SystemThemeMode.Light;
    }
}

