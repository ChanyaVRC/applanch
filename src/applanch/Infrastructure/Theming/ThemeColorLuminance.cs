using System.Windows.Media;

namespace applanch.Infrastructure.Theming;

internal static class ThemeColorLuminance
{
    internal static bool IsDark(Color color)
    {
        var relativeLuminance = ((0.2126 * color.R) + (0.7152 * color.G) + (0.0722 * color.B)) / 255d;
        return relativeLuminance < 0.5d;
    }
}