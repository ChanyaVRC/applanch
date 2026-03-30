using System.Windows.Controls;
using System.Windows.Input;

namespace applanch.Infrastructure.Dialogs;

internal sealed class InlineRenameHandler
{
    internal void HandleVisibleChanged(object sender)
    {
        if (sender is not TextBox { IsVisible: true } textBox)
        {
            return;
        }

        textBox.Focus();
        textBox.SelectAll();
    }

    internal bool HandleKeyDown(object sender, Key key, Action<LaunchItemViewModel, string> applyDisplayName)
    {
        if (sender is not TextBox { DataContext: LaunchItemViewModel item })
        {
            return false;
        }

        if (key == Key.Return)
        {
            CommitRename(item, applyDisplayName);
            return true;
        }

        if (key == Key.Escape)
        {
            item.IsRenaming = false;
            return true;
        }

        return false;
    }

    internal void HandleLostFocus(object sender, Action<LaunchItemViewModel, string> applyDisplayName)
    {
        if (sender is not TextBox { DataContext: LaunchItemViewModel item } || !item.IsRenaming)
        {
            return;
        }

        CommitRename(item, applyDisplayName);
    }

    private static void CommitRename(LaunchItemViewModel item, Action<LaunchItemViewModel, string> applyDisplayName)
    {
        applyDisplayName(item, item.EditingName);
        item.IsRenaming = false;
    }
}
