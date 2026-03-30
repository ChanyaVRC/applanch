using System.Windows;

namespace applanch;

internal readonly record struct MessageDialogVisual(string Symbol, string BrushResourceKey, bool ShowIcon);

internal static class MessageDialogVisuals
{
    public static MessageDialogVisual Resolve(MessageBoxImage icon) => icon switch
    {
        MessageBoxImage.Error => new("\uEA39", "Brush.SurfaceBorder", true),
        MessageBoxImage.Warning => new("\uE7BA", "Brush.SurfaceBorder", true),
        MessageBoxImage.Information => new("\uE946", "Brush.TextSecondary", true),
        MessageBoxImage.Question => new("\uE897", "Brush.TextSecondary", true),
        _ => new(string.Empty, "Brush.TextSecondary", false)
    };
}
