using System.Windows;

namespace applanch.ViewModels;

public sealed class UpdateBannerState : ObservableObject
{
    private string _message = string.Empty;
    private Visibility _bannerVisibility = Visibility.Collapsed;
    private Visibility _headerButtonVisibility = Visibility.Collapsed;

    public string Message
    {
        get => _message;
        internal set => SetField(ref _message, value);
    }

    public Visibility BannerVisibility
    {
        get => _bannerVisibility;
        internal set => SetField(ref _bannerVisibility, value);
    }

    public Visibility HeaderButtonVisibility
    {
        get => _headerButtonVisibility;
        internal set => SetField(ref _headerButtonVisibility, value);
    }
}
