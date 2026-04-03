using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Threading;

namespace applanch.Infrastructure.Utilities;

internal sealed class FloatingNotificationPresenter : IDisposable
{
    private readonly FrameworkElement _storyboardHost;
    private readonly FrameworkElement _banner;
    private readonly ScaleTransform _progressScale;
    private readonly Storyboard _slideInStoryboard;
    private readonly Storyboard _slideOutStoryboard;
    private readonly Storyboard _countdownStoryboard;
    private readonly DispatcherTimer _timer;
    private readonly FloatingNotificationCoordinator _coordinator;
    private readonly Action _clearNotification;

    internal FloatingNotificationPresenter(
        FrameworkElement storyboardHost,
        FrameworkElement banner,
        ScaleTransform progressScale,
        Storyboard slideInStoryboard,
        Storyboard slideOutStoryboard,
        Storyboard countdownStoryboard,
        TimeSpan duration,
        FloatingNotificationCoordinator coordinator,
        Action clearNotification)
    {
        _storyboardHost = storyboardHost;
        _banner = banner;
        _progressScale = progressScale;
        _slideInStoryboard = slideInStoryboard;
        _slideOutStoryboard = slideOutStoryboard;
        _countdownStoryboard = countdownStoryboard;
        _coordinator = coordinator;
        _clearNotification = clearNotification;
        _timer = new DispatcherTimer
        {
            Interval = duration
        };
        _timer.Tick += OnTimerTick;
        _slideOutStoryboard.Completed += OnHideAnimationCompleted;
    }

    internal static FloatingNotificationPresenter Create(
        Window window,
        FrameworkElement banner,
        ScaleTransform progressScale,
        TimeSpan duration,
        Action clearNotification)
    {
        return new FloatingNotificationPresenter(
            window,
            banner,
            progressScale,
            (Storyboard)window.Resources["FloatingNotificationSlideInStoryboard"],
            (Storyboard)window.Resources["FloatingNotificationSlideOutStoryboard"],
            (Storyboard)window.Resources["FloatingNotificationCountdownStoryboard"],
            duration,
            new FloatingNotificationCoordinator(),
            clearNotification);
    }

    internal void Show()
    {
        _banner.Visibility = Visibility.Visible;
        _coordinator.BeginShow();
        _slideInStoryboard.Begin(_storyboardHost, HandoffBehavior.SnapshotAndReplace, isControllable: true);
        _countdownStoryboard.Begin(_storyboardHost, HandoffBehavior.SnapshotAndReplace, isControllable: true);
        _timer.Stop();
        _timer.Start();
    }

    internal void Hide()
    {
        var isBannerVisible = _banner.Visibility == Visibility.Visible;
        var shouldAnimateHide = _coordinator.BeginHide(isBannerVisible);
        _timer.Stop();

        if (isBannerVisible)
        {
            var frozenProgressScale = FloatingNotificationProgressState.CaptureVisibleScale(_progressScale.ScaleX);
            _countdownStoryboard.Stop(_storyboardHost);
            _progressScale.ScaleX = frozenProgressScale;
        }

        if (!shouldAnimateHide)
        {
            _clearNotification();
            return;
        }

        _slideOutStoryboard.Begin(_storyboardHost, HandoffBehavior.SnapshotAndReplace, isControllable: true);
    }

    public void Dispose()
    {
        _timer.Stop();
        _timer.Tick -= OnTimerTick;
        _slideOutStoryboard.Completed -= OnHideAnimationCompleted;
    }

    private void OnTimerTick(object? sender, EventArgs e)
    {
        Hide();
    }

    private void OnHideAnimationCompleted(object? sender, EventArgs e)
    {
        if (!_coordinator.CanCompleteHide())
        {
            return;
        }

        _banner.Visibility = Visibility.Collapsed;
        _clearNotification();
    }
}