using System.Windows.Media;

namespace applanch.Theming;

/// <summary>
/// WPF-specific extensions for <see cref="ThemeColor"/>.
/// </summary>
public static class ThemeColorWpfExtensions
{
    /// <summary>
    /// Converts to a WPF <see cref="Color"/>.
    /// </summary>
    public static Color ToMediaColor(this ThemeColor color) => Color.FromArgb(color.A, color.R, color.G, color.B);
}
