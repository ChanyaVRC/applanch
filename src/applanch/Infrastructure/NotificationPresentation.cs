using System.Windows;

namespace applanch.Infrastructure;

internal static class NotificationPresentation
{
    internal static (string BackgroundKey, string BorderKey) GetFloatingStyleKeys(MessageBoxImage icon)
    {
        return icon switch
        {
            MessageBoxImage.Error => ("Brush.NotificationErrorBackground", "Brush.NotificationErrorBorder"),
            MessageBoxImage.Warning => ("Brush.NotificationWarningBackground", "Brush.NotificationWarningBorder"),
            _ => ("Brush.NotificationInfoBackground", "Brush.NotificationInfoBorder")
        };
    }

    internal static string GetQuickAddForegroundKey(QuickAddMessageSeverity severity)
    {
        return severity switch
        {
            QuickAddMessageSeverity.Warning => "Brush.QuickAddWarningText",
            _ => "Brush.QuickAddInfoText"
        };
    }
}