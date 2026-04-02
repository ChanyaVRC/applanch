using System.Windows;
using applanch.ViewModels;
using Xunit;

namespace applanch.Tests;

public class DragReorderStateTests
{
    [Fact]
    public void Clear_ResetsDraggedItemAndPreviewIndex()
    {
        var state = new DragReorderState
        {
            DragStartPoint = new Point(10, 20),
            DraggedItem = new LaunchItemViewModel("path", "Dev", string.Empty, "App"),
            LastDragPreviewIndex = 3,
        };

        state.Clear();

        Assert.Null(state.DraggedItem);
        Assert.Null(state.LastDragPreviewIndex);
        Assert.Equal(new Point(10, 20), state.DragStartPoint);
    }

    [Fact]
    public void ConsumeShouldPersistOrder_WhenPreviewExists_ReturnsTrueAndClears()
    {
        var state = new DragReorderState
        {
            LastDragPreviewIndex = 1,
            DraggedItem = new LaunchItemViewModel("path", "Dev", string.Empty, "App"),
        };

        var shouldPersist = state.ConsumeShouldPersistOrder();

        Assert.True(shouldPersist);
        Assert.Null(state.LastDragPreviewIndex);
        Assert.Null(state.DraggedItem);
    }

    [Fact]
    public void ConsumeShouldPersistOrder_WhenNoPreview_ReturnsFalseAndClears()
    {
        var state = new DragReorderState
        {
            LastDragPreviewIndex = null,
            DraggedItem = new LaunchItemViewModel("path", "Dev", string.Empty, "App"),
        };

        var shouldPersist = state.ConsumeShouldPersistOrder();

        Assert.False(shouldPersist);
        Assert.Null(state.LastDragPreviewIndex);
        Assert.Null(state.DraggedItem);
    }
}
