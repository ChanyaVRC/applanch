using System.Windows;
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
}
