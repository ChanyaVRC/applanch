using System.Windows;

namespace applanch.ViewModels;

internal sealed class DragReorderState
{
    internal Point DragStartPoint { get; set; }

    internal LaunchItemViewModel? DraggedItem { get; set; }

    internal int? LastDragPreviewIndex { get; set; }

    internal void Clear()
    {
        DraggedItem = null;
        LastDragPreviewIndex = null;
    }

    internal bool ConsumeShouldPersistOrder()
    {
        var shouldPersist = LastDragPreviewIndex.HasValue;
        Clear();
        return shouldPersist;
    }
}
