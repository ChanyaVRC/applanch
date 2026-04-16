using System.Windows.Controls;
using System.Windows.Input;
using applanch.ViewModels;

namespace applanch.Workflows.Items;

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

    internal bool HandleKeyDown(object sender, Key key)
    {
        if (sender is not TextBox { DataContext: LaunchItemViewModel item })
        {
            return false;
        }

        if (key == Key.Return)
        {
            CommitRename(item);
            return true;
        }

        if (key == Key.Escape)
        {
            item.IsRenaming = false;
            return true;
        }

        return false;
    }

    internal void HandleLostFocus(object sender)
    {
        if (sender is not TextBox { DataContext: LaunchItemViewModel item } || !item.IsRenaming)
        {
            return;
        }

        CommitRename(item);
    }

    private static void CommitRename(LaunchItemViewModel item)
    {
        item.DisplayName = item.EditingName;
        item.IsRenaming = false;
    }
}
