using System.Windows;
using applanch.Infrastructure.Storage;
using applanch.Infrastructure.Updates;

namespace applanch.ViewModels;

public sealed class UpdateBannerState : ObservableObject
{
    private string _message = string.Empty;
    private Visibility _bannerVisibility = Visibility.Collapsed;
    private Visibility _headerButtonVisibility = Visibility.Collapsed;
    private Visibility _actionButtonVisibility = Visibility.Visible;
    private bool _areUpdateActionsEnabled = true;
    private AppUpdateInfo? _pendingUpdate;

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

    public Visibility ActionButtonVisibility
    {
        get => _actionButtonVisibility;
        internal set => SetField(ref _actionButtonVisibility, value);
    }

    public bool AreUpdateActionsEnabled
    {
        get => _areUpdateActionsEnabled;
        private set => SetField(ref _areUpdateActionsEnabled, value);
    }

    internal AppUpdateInfo? PendingUpdate => _pendingUpdate;

    internal void ApplyAvailability(AppUpdateInfo? update, UpdateInstallBehavior behavior)
    {
        _pendingUpdate = update;

        if (update is null)
        {
            Message = string.Empty;
            BannerVisibility = Visibility.Collapsed;
            HeaderButtonVisibility = Visibility.Collapsed;
            ActionButtonVisibility = Visibility.Visible;
            AreUpdateActionsEnabled = true;
            return;
        }

        Message = string.Format(AppResources.UpdateMessage, update.NewVersion, update.CurrentVersion);
        var presentation = ResolvePresentation(behavior);
        BannerVisibility = presentation.BannerVisibility;
        HeaderButtonVisibility = presentation.HeaderButtonVisibility;
        ActionButtonVisibility = presentation.ActionButtonVisibility;
    }

    internal void RevealManualActions()
    {
        BannerVisibility = Visibility.Visible;
        HeaderButtonVisibility = Visibility.Visible;
        ActionButtonVisibility = Visibility.Visible;
    }

    internal void Dismiss() => BannerVisibility = Visibility.Collapsed;

    internal bool TryBeginUpdateApply()
    {
        if (!AreUpdateActionsEnabled)
        {
            return false;
        }

        AreUpdateActionsEnabled = false;
        return true;
    }

    internal void EndUpdateApply()
    {
        AreUpdateActionsEnabled = true;
    }

    internal static UpdateBannerPresentation ResolvePresentation(UpdateInstallBehavior behavior)
    {
        return behavior switch
        {
            UpdateInstallBehavior.NotifyOnly => new UpdateBannerPresentation(
                Visibility.Visible,
                Visibility.Collapsed,
                Visibility.Collapsed),
            UpdateInstallBehavior.AutomaticallyApply => new UpdateBannerPresentation(
                Visibility.Collapsed,
                Visibility.Collapsed,
                Visibility.Collapsed),
            _ => new UpdateBannerPresentation(
                Visibility.Visible,
                Visibility.Visible,
                Visibility.Visible),
        };
    }
}
