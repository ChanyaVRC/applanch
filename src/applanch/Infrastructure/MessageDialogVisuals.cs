using System.Windows;

namespace applanch;

internal readonly record struct MessageDialogVisual(string Symbol, string BrushResourceKey, bool ShowIcon);

internal static class MessageDialogVisuals
{
    public static MessageDialogVisual Resolve(MessageBoxImage icon) => icon switch
    {
        MessageBoxImage.Error => new("X", "Brush.SurfaceBorder", true),
        MessageBoxImage.Warning => new("!", "Brush.SurfaceBorder", true),
        MessageBoxImage.Information => new(string.Empty, "Brush.TextSecondary", false),
        MessageBoxImage.Question => new("?", "Brush.TextSecondary", true),
        _ => new(string.Empty, "Brush.TextSecondary", false)
    };
}
