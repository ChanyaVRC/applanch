using Microsoft.Win32;
using System.Windows;
using System.Windows.Media;

namespace applanch;

internal sealed class ThemeManager(Func<bool>? isLightThemeProvider = null)
{
    private const string PersonalizeRegistryPath = @"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize";
    private const string AppsUseLightTheme = "AppsUseLightTheme";

    private static readonly (string Key, string LightHex, string DarkHex)[] Palette =
    [
        ("Brush.AppBackground", "#F1F5F9", "#0B1220"),
        ("Brush.Surface", "#FFFFFF", "#131D31"),
        ("Brush.SurfaceBorder", "#D0D7E2", "#223149"),
        ("Brush.TextPrimary", "#0F172A", "#E2E8F0"),
        ("Brush.TextSecondary", "#475569", "#9FB2C9"),
        ("Brush.TextTertiary", "#64748B", "#7C93AF"),
        ("Brush.ItemBackground", "#F8FAFC", "#111C30"),
        ("Brush.ItemBorder", "#D7DEE8", "#2A3B57"),
        ("Brush.IconBackground", "#E2E8F0", "#20304B")
    ];

    private readonly Func<bool> _isLightThemeProvider = isLightThemeProvider ?? ReadWindowsThemePreference;

    public void ApplyTheme(ResourceDictionary resources)
    {
        var isLight = _isLightThemeProvider();

        foreach (var (key, lightHex, darkHex) in Palette)
        {
            var hex = isLight ? lightHex : darkHex;
            SetBrush(resources, key, ColorFromHex(hex));
        }
    }

    private static void SetBrush(ResourceDictionary resources, string key, Color color)
    {
        resources[key] = new SolidColorBrush(color);
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
