using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Animation;
using System.Windows.Threading;
using applanch.Infrastructure.Utilities;
using applanch.ViewModels;

namespace applanch.Controls;

public sealed partial class FloatingNotificationControl : UserControl
{
    private int _animationVersion;
    private int _expectedHideVersion;
    private readonly DispatcherTimer _timer;
    private readonly Storyboard _slideInStoryboard;
    private readonly Storyboard _slideOutStoryboard;
    private readonly Storyboard _countdownStoryboard;

    public FloatingNotificationControl()
    {
        InitializeComponent();
        _timer = new DispatcherTimer
        {
            Interval = Duration
        };
        _timer.Tick += OnTimerTick;
        _slideInStoryboard = (Storyboard)Resources["FloatingNotificationSlideInStoryboard"];
        _slideOutStoryboard = (Storyboard)Resources["FloatingNotificationSlideOutStoryboard"];
        _countdownStoryboard = (Storyboard)Resources["FloatingNotificationCountdownStoryboard"];
        _slideOutStoryboard.Completed += OnHideAnimationCompleted;
        Unloaded += OnUnloaded;
    }

    public static readonly DependencyProperty MessageProperty =
        DependencyProperty.Register(
            nameof(Message),
            typeof(string),
            typeof(FloatingNotificationControl),
            new PropertyMetadata(string.Empty));

    public static readonly DependencyProperty IconTypeProperty =
        DependencyProperty.Register(
            nameof(IconType),
            typeof(NotificationIconType),
            typeof(FloatingNotificationControl),
            new PropertyMetadata(NotificationIconType.None));

    public static readonly DependencyProperty ActionTextProperty =
        DependencyProperty.Register(
            nameof(ActionText),
            typeof(string),
            typeof(FloatingNotificationControl),
            new PropertyMetadata(string.Empty));

    public static readonly DependencyProperty ActionVisibilityProperty =
        DependencyProperty.Register(
            nameof(ActionVisibility),
            typeof(Visibility),
            typeof(FloatingNotificationControl),
            new PropertyMetadata(Visibility.Collapsed));

    public static readonly DependencyProperty DurationProperty =
        DependencyProperty.Register(
            nameof(Duration),
            typeof(TimeSpan),
            typeof(FloatingNotificationControl),
            new PropertyMetadata(TimeSpan.FromSeconds(4), OnDurationChanged));

    public string Message
    {
        get => (string)GetValue(MessageProperty);
        set => SetValue(MessageProperty, value);
    }

    public NotificationIconType IconType
    {
        get => (NotificationIconType)GetValue(IconTypeProperty);
        set => SetValue(IconTypeProperty, value);
    }

    public string ActionText
    {
        get => (string)GetValue(ActionTextProperty);
        set => SetValue(ActionTextProperty, value);
    }

    public Visibility ActionVisibility
    {
        get => (Visibility)GetValue(ActionVisibilityProperty);
        set => SetValue(ActionVisibilityProperty, value);
    }

    public TimeSpan Duration
    {
        get => (TimeSpan)GetValue(DurationProperty);
        set => SetValue(DurationProperty, value);
    }

    public event RoutedEventHandler? ActionRequested;

    public event RoutedEventHandler? Hidden;

    public void ShowNotification()
    {
        Visibility = Visibility.Visible;
        _animationVersion++;
        _slideInStoryboard.Begin(this, HandoffBehavior.SnapshotAndReplace, isControllable: true);
        _countdownStoryboard.Begin(this, HandoffBehavior.SnapshotAndReplace, isControllable: true);
        _timer.Stop();
        _timer.Start();
    }

    public void HideNotification()
    {
        var isVisible = Visibility == Visibility.Visible;
        _expectedHideVersion = ++_animationVersion;
        _timer.Stop();

        if (isVisible)
        {
            var frozenProgressScale = FloatingNotificationProgressState.CaptureVisibleScale(FloatingNotificationProgressScale.ScaleX);
            _countdownStoryboard.Stop(this);
            FloatingNotificationProgressScale.ScaleX = frozenProgressScale;
        }

        if (!isVisible)
        {
            NotifyHidden();
            return;
        }

        _slideOutStoryboard.Begin(this, HandoffBehavior.SnapshotAndReplace, isControllable: true);
    }

    private static void OnDurationChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not FloatingNotificationControl control || e.NewValue is not TimeSpan duration)
        {
            return;
        }

        control._timer.Interval = duration;
    }

    private void ActionButton_Click(object sender, RoutedEventArgs e)
    {
        ActionRequested?.Invoke(this, e);
    }

    private void OnTimerTick(object? sender, EventArgs e)
    {
        HideNotification();
    }

    private void OnHideAnimationCompleted(object? sender, EventArgs e)
    {
        if (_expectedHideVersion != _animationVersion)
        {
            return;
        }

        NotifyHidden();
    }

    private void NotifyHidden()
    {
        Visibility = Visibility.Collapsed;
        Hidden?.Invoke(this, new RoutedEventArgs());
    }

    private void OnUnloaded(object sender, RoutedEventArgs e)
    {
        _timer.Stop();
        _timer.Tick -= OnTimerTick;
        _slideOutStoryboard.Completed -= OnHideAnimationCompleted;
        Unloaded -= OnUnloaded;
    }
}