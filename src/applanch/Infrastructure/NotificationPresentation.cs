using System.Windows;
using System.Windows.Media;

namespace applanch.Infrastructure;

internal static class NotificationPresentation
{
    internal static readonly Brush FloatingInfoBackground = CreateBrush("#EE1F2937");
    internal static readonly Brush FloatingInfoBorder = CreateBrush("#334155");
    internal static readonly Brush FloatingWarningBackground = CreateBrush("#EEF59E0B");
    internal static readonly Brush FloatingWarningBorder = CreateBrush("#D97706");
    internal static readonly Brush FloatingErrorBackground = CreateBrush("#EEB91C1C");
    internal static readonly Brush FloatingErrorBorder = CreateBrush("#EF4444");
    internal static readonly Brush QuickAddInfoForeground = CreateBrush("#DC2626");
    internal static readonly Brush QuickAddWarningForeground = CreateBrush("#D97706");

    internal static (Brush Background, Brush BorderBrush) GetFloatingStyle(MessageBoxImage icon)
    {
        return icon switch
        {
            MessageBoxImage.Error => (FloatingErrorBackground, FloatingErrorBorder),
            MessageBoxImage.Warning => (FloatingWarningBackground, FloatingWarningBorder),
            _ => (FloatingInfoBackground, FloatingInfoBorder)
        };
    }

    internal static Brush GetQuickAddForeground(QuickAddMessageSeverity severity)
    {
        return severity switch
        {
            QuickAddMessageSeverity.Warning => QuickAddWarningForeground,
            _ => QuickAddInfoForeground
        };
    }

    private static Brush CreateBrush(string color)
    {
        var brush = (Brush)new BrushConverter().ConvertFromString(color)!;
        brush.Freeze();
        return brush;
    }
}