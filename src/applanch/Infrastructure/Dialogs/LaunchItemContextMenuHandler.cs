using System.Windows;
using System.Windows.Controls;

namespace applanch.Infrastructure.Dialogs;

internal sealed class LaunchItemContextMenuHandler(IUserInteractionService interactionService, Window owner)
{
    internal void EditCategory(
        object sender,
        IEnumerable<string> categoryNames,
        string promptTitle,
        Action<LaunchItemViewModel, string> applyCategory)
    {
        var item = GetTargetItem(sender);
        if (item is null)
        {
            return;
        }

        var suggestions = categoryNames
            .Where(static name => !string.IsNullOrWhiteSpace(name))
            .Distinct(StringComparer.Ordinal)
            .ToArray();

        var newValue = interactionService.PromptWithSuggestions(promptTitle, item.Category, suggestions, owner);
        if (newValue is null)
        {
            return;
        }

        applyCategory(item, newValue);
    }

    internal void EditValue(
        object sender,
        string promptTitle,
        Func<LaunchItemViewModel, string> valueSelector,
        Action<LaunchItemViewModel, string> applyAction)
    {
        var item = GetTargetItem(sender);
        if (item is null)
        {
            return;
        }

        var newValue = interactionService.Prompt(promptTitle, valueSelector(item), owner);
        if (newValue is null)
        {
            return;
        }

        applyAction(item, newValue);
    }

    internal void BeginRename(object sender)
    {
        var item = GetTargetItem(sender);
        if (item is null)
        {
            return;
        }

        item.EditingName = item.DisplayName;
        item.IsRenaming = true;
    }

    internal void Delete(object sender, Action<LaunchItemViewModel> remove)
    {
        var item = GetTargetItem(sender);
        if (item is null)
        {
            return;
        }

        remove(item);
    }

    internal static LaunchItemViewModel? GetTargetItem(object sender)
    {
        if (sender is not MenuItem { Parent: ContextMenu contextMenu })
        {
            return null;
        }

        if (contextMenu.PlacementTarget is not FrameworkElement { DataContext: LaunchItemViewModel item })
        {
            return null;
        }

        return item;
    }
}
