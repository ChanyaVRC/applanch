using System.Windows;
using applanch.ViewModels;
using Xunit;

namespace applanch.Tests.ViewModels;

public class QuickAddFeedbackStateTests
{
    [Fact]
    public void InitialState_MessageIsEmpty_VisibilityIsCollapsed()
    {
        var state = new QuickAddFeedbackState();

        Assert.Equal(string.Empty, state.Message);
        Assert.Equal(Visibility.Collapsed, state.MessageVisibility);
    }

    [Fact]
    public void Message_SetToNonEmpty_VisibilityBecomesVisible()
    {
        var state = new QuickAddFeedbackState();

        state.Message = "Something went wrong";

        Assert.Equal(Visibility.Visible, state.MessageVisibility);
    }

    [Fact]
    public void Message_ClearedAfterSet_VisibilityReverts()
    {
        var state = new QuickAddFeedbackState { Message = "Error" };

        state.Message = string.Empty;

        Assert.Equal(Visibility.Collapsed, state.MessageVisibility);
    }

    [Fact]
    public void Message_Set_RaisesPropertyChangedForMessageAndVisibility()
    {
        var state = new QuickAddFeedbackState();
        var changed = new List<string>();
        state.PropertyChanged += (_, e) => changed.Add(e.PropertyName ?? string.Empty);

        state.Message = "Hello";

        Assert.Contains(nameof(QuickAddFeedbackState.Message), changed);
        Assert.Contains(nameof(QuickAddFeedbackState.MessageVisibility), changed);
    }

    [Fact]
    public void Message_SetToSameValue_DoesNotRaisePropertyChanged()
    {
        var state = new QuickAddFeedbackState { Message = "Same" };
        var changed = new List<string>();
        state.PropertyChanged += (_, e) => changed.Add(e.PropertyName ?? string.Empty);

        state.Message = "Same";

        Assert.Empty(changed);
    }

    [Fact]
    public void Severity_Set_RaisesPropertyChangedForSeverity()
    {
        var state = new QuickAddFeedbackState();
        var changed = new List<string>();
        state.PropertyChanged += (_, e) => changed.Add(e.PropertyName ?? string.Empty);

        state.Severity = QuickAddMessageSeverity.Warning;

        Assert.Contains(nameof(QuickAddFeedbackState.Severity), changed);
    }

    [Fact]
    public void Severity_SetToSameValue_DoesNotRaisePropertyChanged()
    {
        var state = new QuickAddFeedbackState { Severity = QuickAddMessageSeverity.Information };
        var changed = new List<string>();
        state.PropertyChanged += (_, e) => changed.Add(e.PropertyName ?? string.Empty);

        state.Severity = QuickAddMessageSeverity.Information;

        Assert.Empty(changed);
    }
}
