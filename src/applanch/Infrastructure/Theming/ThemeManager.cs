using Microsoft.Win32;
using System.Windows;
using System.Windows.Media;

namespace applanch.Infrastructure.Theming;

internal sealed class ThemeManager(Func<AppTheme>? themeProvider = null)
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
        ("Brush.IconBackground", "#E2E8F0", "#20304B"),
        ("Brush.NotificationInfoBackground", "#FFFFFF", "#131D31"),
        ("Brush.NotificationInfoBorder", "#D7DEE8", "#2A3B57"),
        ("Brush.NotificationWarningBackground", "#FFF7ED", "#2B2111"),
        ("Brush.NotificationWarningBorder", "#FDBA74", "#B45309"),
        ("Brush.NotificationErrorBackground", "#FEF2F2", "#2A1618"),
        ("Brush.NotificationErrorBorder", "#FCA5A5", "#B45353"),
        ("Brush.NotificationProgressTrack", "#E2E8F0", "#2A3B57"),
        ("Brush.NotificationProgressValue", "#94A3B8", "#7C93AF"),
        ("Brush.QuickAddInfoText", "#B45309", "#FBBF24"),
        ("Brush.QuickAddWarningText", "#92400E", "#F59E0B")
    ];

    private static readonly Dictionary<string, SolidColorBrush> LightBrushes = BuildBrushMap(isLight: true);
    private static readonly Dictionary<string, SolidColorBrush> DarkBrushes = BuildBrushMap(isLight: false);

    private readonly Func<AppTheme> _themeProvider = themeProvider ?? (() => AppTheme.System);

    public void ApplyTheme(ResourceDictionary resources)
    {
        var isLight = _themeProvider() switch
        {
            AppTheme.Light => true,
            AppTheme.Dark => false,
            _ => ReadWindowsThemePreference(),
        };

        var brushMap = isLight ? LightBrushes : DarkBrushes;

        foreach (var (key, _, _) in Palette)
        {
            resources[key] = brushMap[key];
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

    private static Dictionary<string, SolidColorBrush> BuildBrushMap(bool isLight)
    {
        var map = new Dictionary<string, SolidColorBrush>(Palette.Length, StringComparer.Ordinal);
        foreach (var (key, lightHex, darkHex) in Palette)
        {
            var color = ColorFromHex(isLight ? lightHex : darkHex);
            var brush = new SolidColorBrush(color);
            brush.Freeze();
            map[key] = brush;
        }

        return map;
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

