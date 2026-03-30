using System.Windows;

namespace applanch;

internal readonly record struct MessageDialogVisual(string Symbol, string BrushResourceKey);

internal static class MessageDialogVisuals
{
    public static MessageDialogVisual Resolve(MessageBoxImage icon) => icon switch
    {
        MessageBoxImage.Error => new("X", "Brush.SurfaceBorder"),
        MessageBoxImage.Warning => new("!", "Brush.SurfaceBorder"),
        MessageBoxImage.Information => new("i", "Brush.TextSecondary"),
        MessageBoxImage.Question => new("?", "Brush.TextSecondary"),
        _ => new(string.Empty, "Brush.TextSecondary")
    };
}
