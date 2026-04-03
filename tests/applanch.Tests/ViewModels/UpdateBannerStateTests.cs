using System.Windows;
using applanch.Infrastructure.Storage;
using applanch.Infrastructure.Updates;
using applanch.ViewModels;
using Xunit;

namespace applanch.Tests.ViewModels;

public class UpdateBannerStateTests
{
    [Fact]
    public void InitialState_AllPropertiesHaveDefaults()
    {
        var state = new UpdateBannerState();

        Assert.Equal(string.Empty, state.Message);
        Assert.Equal(Visibility.Collapsed, state.BannerVisibility);
        Assert.Equal(Visibility.Collapsed, state.HeaderButtonVisibility);
    }

    [Fact]
    public void Message_Set_RaisesPropertyChanged()
    {
        var state = new UpdateBannerState();
        var changed = new List<string>();
        state.PropertyChanged += (_, e) => changed.Add(e.PropertyName ?? string.Empty);

        state.Message = "Update available";

        Assert.Contains(nameof(UpdateBannerState.Message), changed);
    }

    [Fact]
    public void BannerVisibility_Set_RaisesPropertyChanged()
    {
        var state = new UpdateBannerState();
        var changed = new List<string>();
        state.PropertyChanged += (_, e) => changed.Add(e.PropertyName ?? string.Empty);

        state.BannerVisibility = Visibility.Visible;

        Assert.Contains(nameof(UpdateBannerState.BannerVisibility), changed);
    }

    [Fact]
    public void HeaderButtonVisibility_Set_RaisesPropertyChanged()
    {
        var state = new UpdateBannerState();
        var changed = new List<string>();
        state.PropertyChanged += (_, e) => changed.Add(e.PropertyName ?? string.Empty);

        state.HeaderButtonVisibility = Visibility.Visible;

        Assert.Contains(nameof(UpdateBannerState.HeaderButtonVisibility), changed);
    }

    [Fact]
    public void Message_SetToSameValue_DoesNotRaisePropertyChanged()
    {
        var state = new UpdateBannerState { Message = "Same" };
        var changed = new List<string>();
        state.PropertyChanged += (_, e) => changed.Add(e.PropertyName ?? string.Empty);

        state.Message = "Same";

        Assert.Empty(changed);
    }

    [Fact]
    public void BannerVisibility_SetToSameValue_DoesNotRaisePropertyChanged()
    {
        var state = new UpdateBannerState { BannerVisibility = Visibility.Visible };
        var changed = new List<string>();
        state.PropertyChanged += (_, e) => changed.Add(e.PropertyName ?? string.Empty);

        state.BannerVisibility = Visibility.Visible;

        Assert.Empty(changed);
    }

    [Fact]
    public void ApplyAvailability_AutoMode_FirstObservation_MarksAutoApplyPending()
    {
        var state = new UpdateBannerState();

        state.ApplyAvailability(CreateUpdate("1.2.0"), UpdateInstallBehavior.AutomaticallyApply);

        Assert.True(state.ShouldAutoApplyPendingUpdate);
    }

    [Fact]
    public void ApplyAvailability_SameVersionAfterAttempt_ClearsAutoApplyPending()
    {
        var state = new UpdateBannerState();
        state.ApplyAvailability(CreateUpdate("1.2.0"), UpdateInstallBehavior.AutomaticallyApply);

        state.ApplyAvailability(CreateUpdate("1.2.0"), UpdateInstallBehavior.AutomaticallyApply);

        Assert.False(state.ShouldAutoApplyPendingUpdate);
    }

    [Fact]
    public void ApplyAvailability_WhileAutomaticApplyRunning_ClearsAutoApplyPending()
    {
        var state = new UpdateBannerState();
        state.BeginAutomaticApply();

        state.ApplyAvailability(CreateUpdate("1.2.0"), UpdateInstallBehavior.AutomaticallyApply);

        Assert.False(state.ShouldAutoApplyPendingUpdate);
    }

    [Fact]
    public void ApplyAvailability_WithNullUpdate_ResetsPendingUpdateBannerAndAutoApplyFlag()
    {
        var state = new UpdateBannerState();
        state.ApplyAvailability(CreateUpdate("1.2.0"), UpdateInstallBehavior.Manual);

        state.ApplyAvailability(null, UpdateInstallBehavior.Manual);

        Assert.Null(state.PendingUpdate);
        Assert.Equal(Visibility.Collapsed, state.BannerVisibility);
        Assert.False(state.ShouldAutoApplyPendingUpdate);
    }

    private static AppUpdateInfo CreateUpdate(string version)
    {
        return new AppUpdateInfo(
            version,
            "1.0.0",
            new Uri("https://example.com/download.zip"),
            new Uri("https://example.com/release"));
    }
}
