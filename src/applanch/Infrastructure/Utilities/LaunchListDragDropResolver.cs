using System.Diagnostics.CodeAnalysis;
using System.Windows;
using System.Windows.Controls;
using applanch.ViewModels;

namespace applanch.Infrastructure.Utilities;

internal sealed class LaunchListDragDropResolver
{
    internal bool TryGetDraggedItemData(
        IDataObject data,
        IReadOnlyList<LaunchItemViewModel> items,
        [NotNullWhen(true)] out LaunchItemViewModel? draggedItem,
        out int oldIndex)
    {
        draggedItem = null;
        oldIndex = -1;

        if (!data.GetDataPresent(typeof(LaunchItemViewModel)) ||
            data.GetData(typeof(LaunchItemViewModel)) is not LaunchItemViewModel item)
        {
            return false;
        }

        var index = FindIndex(items, item);
        if (index < 0)
        {
            return false;
        }

        draggedItem = item;
        oldIndex = index;
        return true;
    }

    internal int GetDropIndex(ListBox listBox, IReadOnlyList<LaunchItemViewModel> items, int oldIndex, Point listPosition)
    {
        var count = items.Count;
        if (count <= 1)
        {
            return oldIndex;
        }

        var desiredInsertIndex = ResolveDesiredInsertIndex(listBox, items, oldIndex, listPosition);
        return DragReorderIndexCalculator.Calculate(oldIndex, desiredInsertIndex, count);
    }

    private static int ResolveDesiredInsertIndex(
        ListBox listBox,
        IReadOnlyList<LaunchItemViewModel> items,
        int oldIndex,
        Point listPosition)
    {
        if (listPosition.Y <= 0)
        {
            return 0;
        }

        if (listPosition.Y >= listBox.ActualHeight)
        {
            return items.Count;
        }

        var targetContainer = listBox.InputHitTest(listPosition) is DependencyObject hit
            ? VisualTreeUtilities.FindAncestor<ListBoxItem>(hit)
            : null;
        if (targetContainer?.DataContext is not LaunchItemViewModel targetData)
        {
            return oldIndex;
        }

        var targetIndex = FindIndex(items, targetData);
        if (targetIndex < 0)
        {
            return oldIndex;
        }

        var containerOrigin = targetContainer.TranslatePoint(new Point(0, 0), listBox);
        var dropOnItem = new Point(listPosition.X - containerOrigin.X, listPosition.Y - containerOrigin.Y);
        var insertAfter = dropOnItem.Y > targetContainer.ActualHeight / 2;

        return insertAfter ? targetIndex + 1 : targetIndex;
    }

    private static int FindIndex(IReadOnlyList<LaunchItemViewModel> items, LaunchItemViewModel target)
    {
        for (var i = 0; i < items.Count; i++)
        {
            if (ReferenceEquals(items[i], target))
            {
                return i;
            }
        }

        return -1;
    }

}
