using System.Windows.Media;

namespace applanch.Theming;

/// <summary>
/// Utilities for calculating color luminance and contrast.
/// </summary>
internal static class ThemeColorLuminance
{
    /// <summary>
    /// Determines if a color is dark (luminance &lt; 50%).
    /// </summary>
    internal static bool IsDark(Color color)
    {
        var relativeLuminance = ((0.2126 * color.R) + (0.7152 * color.G) + (0.0722 * color.B)) / 255d;
        return relativeLuminance < 0.5d;
    }
}
