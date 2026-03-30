using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;

namespace applanch.Infrastructure.Theming;

internal static class WindowCaptionThemeHelper
{
    private const int DwmaUseImmersiveDarkMode = 20;
    private const int DwmaUseImmersiveDarkModeLegacy = 19;

    [DllImport("dwmapi.dll")]
    private static extern int DwmSetWindowAttribute(IntPtr hwnd, int dwAttribute, ref int pvAttribute, int cbAttribute);

    public static void Apply(Window window)
    {
        var helper = new WindowInteropHelper(window);
        if (helper.Handle == IntPtr.Zero)
        {
            return;
        }

        var isDark = IsDarkTheme(window);
        var darkMode = isDark ? 1 : 0;

        // Newer Windows versions use attribute 20, older builds use 19.
        var hr = DwmSetWindowAttribute(helper.Handle, DwmaUseImmersiveDarkMode, ref darkMode, sizeof(int));
        if (hr != 0)
        {
            _ = DwmSetWindowAttribute(helper.Handle, DwmaUseImmersiveDarkModeLegacy, ref darkMode, sizeof(int));
        }
    }

    private static bool IsDarkTheme(FrameworkElement element)
    {
        if (element.TryFindResource("Brush.AppBackground") is not SolidColorBrush brush)
        {
            return false;
        }

        var c = brush.Color;
        var luminance = (0.2126 * c.R + 0.7152 * c.G + 0.0722 * c.B) / 255.0;
        return luminance < 0.5;
    }
}
