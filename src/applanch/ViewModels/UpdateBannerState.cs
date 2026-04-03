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
    private AppUpdateInfo? _pendingUpdate;
    private string? _lastAutoApplyAttemptedVersion;
    private bool _isAutoApplyingUpdate;
    private bool _shouldAutoApplyPendingUpdate;

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

    internal bool ShouldAutoApplyPendingUpdate
    {
        get => _shouldAutoApplyPendingUpdate;
        private set => SetField(ref _shouldAutoApplyPendingUpdate, value);
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
            _lastAutoApplyAttemptedVersion = null;
            ShouldAutoApplyPendingUpdate = false;
            return;
        }

        Message = string.Format(AppResources.UpdateMessage, update.NewVersion, update.CurrentVersion);
        var presentation = ResolvePresentation(behavior);
        BannerVisibility = presentation.BannerVisibility;
        HeaderButtonVisibility = presentation.HeaderButtonVisibility;
        ActionButtonVisibility = presentation.ActionButtonVisibility;

        if (behavior != UpdateInstallBehavior.AutomaticallyApply)
        {
            ShouldAutoApplyPendingUpdate = false;
            return;
        }

        if (_isAutoApplyingUpdate)
        {
            ShouldAutoApplyPendingUpdate = false;
            return;
        }

        if (string.Equals(_lastAutoApplyAttemptedVersion, update.NewVersion, StringComparison.Ordinal))
        {
            ShouldAutoApplyPendingUpdate = false;
            return;
        }

        _lastAutoApplyAttemptedVersion = update.NewVersion;
        ShouldAutoApplyPendingUpdate = true;
    }

    internal void RevealManualActions()
    {
        BannerVisibility = Visibility.Visible;
        HeaderButtonVisibility = Visibility.Visible;
        ActionButtonVisibility = Visibility.Visible;
    }

    internal void Dismiss() => BannerVisibility = Visibility.Collapsed;

    internal void BeginAutomaticApply()
    {
        _isAutoApplyingUpdate = true;
    }

    internal void EndAutomaticApply()
    {
        _isAutoApplyingUpdate = false;
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
