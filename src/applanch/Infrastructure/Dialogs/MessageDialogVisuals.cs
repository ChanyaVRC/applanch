using System.Windows;
using applanch.Theming;

namespace applanch.Infrastructure.Dialogs;

internal static class MessageDialogVisuals
{
    public static MessageDialogVisual Resolve(MessageBoxImage icon) => icon switch
    {
        MessageBoxImage.Error => new("\uEA39", ThemeBrushKey.DialogError.ToResourceKey(), true),
        MessageBoxImage.Warning => new("\uE7BA", ThemeBrushKey.DialogWarning.ToResourceKey(), true),
        MessageBoxImage.Information => new("\uE946", ThemeBrushKey.DialogInfo.ToResourceKey(), true),
        MessageBoxImage.Question => new("\uE897", ThemeBrushKey.DialogQuestion.ToResourceKey(), true),
        _ => new(string.Empty, ThemeBrushKey.TextSecondary.ToResourceKey(), false)
    };
}

