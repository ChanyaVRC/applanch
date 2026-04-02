using System.Windows;
using applanch.ViewModels;
using Xunit;

namespace applanch.Tests;

public class FloatingNotificationStateTests
{
    [Fact]
    public void UndoVisibility_IsCollapsed_WhenUndoActionIsNull()
    {
        var state = new FloatingNotificationState();

        Assert.Equal(Visibility.Collapsed, state.UndoVisibility);
    }

    [Fact]
    public void UndoVisibility_IsVisible_WhenUndoActionIsSet()
    {
        var state = new FloatingNotificationState
        {
            UndoAction = static () => { }
        };

        Assert.Equal(Visibility.Visible, state.UndoVisibility);
    }

    [Fact]
    public void UndoAction_Set_RaisesPropertyChanged_ForUndoActionAndUndoVisibility()
    {
        var state = new FloatingNotificationState();
        var changed = new List<string>();
        state.PropertyChanged += (_, e) => changed.Add(e.PropertyName ?? string.Empty);

        state.UndoAction = static () => { };

        Assert.Contains(nameof(FloatingNotificationState.UndoAction), changed);
        Assert.Contains(nameof(FloatingNotificationState.UndoVisibility), changed);
    }

    [Fact]
    public void UndoAction_Cleared_RaisesPropertyChanged_ForUndoActionAndUndoVisibility()
    {
        var state = new FloatingNotificationState
        {
            UndoAction = static () => { }
        };
        var changed = new List<string>();
        state.PropertyChanged += (_, e) => changed.Add(e.PropertyName ?? string.Empty);

        state.UndoAction = null;

        Assert.Contains(nameof(FloatingNotificationState.UndoAction), changed);
        Assert.Contains(nameof(FloatingNotificationState.UndoVisibility), changed);
    }

    [Fact]
    public void DeleteVisibility_IsCollapsed_WhenDeleteActionIsNull()
    {
        var state = new FloatingNotificationState();

        Assert.Equal(Visibility.Collapsed, state.DeleteVisibility);
    }

    [Fact]
    public void DeleteVisibility_IsVisible_WhenDeleteActionIsSet()
    {
        var state = new FloatingNotificationState
        {
            DeleteAction = static () => { }
        };

        Assert.Equal(Visibility.Visible, state.DeleteVisibility);
    }

    [Fact]
    public void DeleteAction_Set_RaisesPropertyChanged_ForDeleteActionAndDeleteVisibility()
    {
        var state = new FloatingNotificationState();
        var changed = new List<string>();
        state.PropertyChanged += (_, e) => changed.Add(e.PropertyName ?? string.Empty);

        state.DeleteAction = static () => { };

        Assert.Contains(nameof(FloatingNotificationState.DeleteAction), changed);
        Assert.Contains(nameof(FloatingNotificationState.DeleteVisibility), changed);
    }
}
