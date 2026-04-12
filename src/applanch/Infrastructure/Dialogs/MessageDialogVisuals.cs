using System.Windows;

namespace applanch.Infrastructure.Dialogs;

internal static class MessageDialogVisuals
{
    public static MessageDialogVisual Resolve(MessageBoxImage icon) => icon switch
    {
        MessageBoxImage.Error => new("\uEA39", "Brush.DialogError", true),
        MessageBoxImage.Warning => new("\uE7BA", "Brush.DialogWarning", true),
        MessageBoxImage.Information => new("\uE946", "Brush.DialogInfo", true),
        MessageBoxImage.Question => new("\uE897", "Brush.DialogQuestion", true),
        _ => new(string.Empty, "Brush.TextSecondary", false)
    };
}

