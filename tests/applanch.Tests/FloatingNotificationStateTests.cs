using System.Windows;
using applanch.ViewModels;
using Xunit;

namespace applanch.Tests;

public class FloatingNotificationStateTests
{
    [Fact]
    public void ActionVisibility_IsCollapsed_WhenActionIsNull()
    {
        var state = new FloatingNotificationState();

        Assert.Equal(Visibility.Collapsed, state.ActionVisibility);
    }

    [Fact]
    public void ActionVisibility_IsVisible_WhenActionIsSet()
    {
        var state = new FloatingNotificationState
        {
            Action = static () => { }
        };

        Assert.Equal(Visibility.Visible, state.ActionVisibility);
    }

    [Fact]
    public void Action_Set_RaisesPropertyChanged_ForActionAndActionVisibility()
    {
        var state = new FloatingNotificationState();
        var changed = new List<string>();
        state.PropertyChanged += (_, e) => changed.Add(e.PropertyName ?? string.Empty);

        state.Action = static () => { };

        Assert.Contains(nameof(FloatingNotificationState.Action), changed);
        Assert.Contains(nameof(FloatingNotificationState.ActionVisibility), changed);
    }

    [Fact]
    public void Action_Cleared_RaisesPropertyChanged_ForActionAndActionVisibility()
    {
        var state = new FloatingNotificationState
        {
            Action = static () => { }
        };
        var changed = new List<string>();
        state.PropertyChanged += (_, e) => changed.Add(e.PropertyName ?? string.Empty);

        state.Action = null;

        Assert.Contains(nameof(FloatingNotificationState.Action), changed);
        Assert.Contains(nameof(FloatingNotificationState.ActionVisibility), changed);
    }

    [Fact]
    public void Show_SetsActionText_AndClear_ResetsIt()
    {
        var state = new FloatingNotificationState();

        state.Show("message", NotificationIconType.Info, "Undo", static () => { });

        Assert.Equal("Undo", state.ActionText);
        Assert.Equal(Visibility.Visible, state.ActionVisibility);

        state.Clear();

        Assert.Equal(string.Empty, state.ActionText);
        Assert.Equal(Visibility.Collapsed, state.ActionVisibility);
    }
}
