using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Threading;
using applanch.Infrastructure.Dialogs;
using applanch.Infrastructure.Launch;
using applanch.Infrastructure.Storage;
using applanch.Infrastructure.Theming;
using applanch.Infrastructure.Updates;
using applanch.Infrastructure.Utilities;
using Strings = applanch.Properties.Resources;

namespace applanch;

public partial class MainWindow : Window
{
    private static readonly TimeSpan FloatingNotificationDuration = TimeSpan.FromSeconds(4);
    private readonly DragReorderState _dragReorderState = new();
    private readonly IItemLaunchService _itemLaunchService;
    private readonly IUserInteractionService _interactionService;
    private readonly LaunchItemContextMenuHandler _contextMenuHandler;
    private readonly InlineRenameHandler _inlineRenameHandler;
    private readonly LaunchListDragDropResolver _dragDropResolver;
    private readonly UpdateWorkflow _updateWorkflow;
    private readonly FloatingNotificationCoordinator _floatingNotificationCoordinator;
    private AppSettings _settings;
    private AppUpdateInfo? _pendingUpdate;
    private readonly DispatcherTimer _floatingNotificationTimer;
    private readonly Storyboard _slideInStoryboard;
    private readonly Storyboard _slideOutStoryboard;
    private readonly Storyboard _countdownStoryboard;
    private MainWindowViewModel ViewModel { get; }

    public MainWindow()
        : this(new MainWindowViewModel(), new ItemLaunchService(), new UserInteractionService(), new GitHubAppUpdateService())
    {
    }

    internal MainWindow(MainWindowViewModel viewModel, IItemLaunchService itemLaunchService, IUserInteractionService interactionService, IAppUpdateService updateService)
    {
        InitializeComponent();
        ViewModel = viewModel;
        _itemLaunchService = itemLaunchService;
        _interactionService = interactionService;
        _contextMenuHandler = new LaunchItemContextMenuHandler(_interactionService, this);
        _inlineRenameHandler = new InlineRenameHandler();
        _dragDropResolver = new LaunchListDragDropResolver();
        _updateWorkflow = new UpdateWorkflow(updateService);
        _floatingNotificationCoordinator = new FloatingNotificationCoordinator();
        _settings = AppSettings.Load();
        _floatingNotificationTimer = new DispatcherTimer
        {
            Interval = FloatingNotificationDuration
        };
        _floatingNotificationTimer.Tick += FloatingNotificationTimer_Tick;
        _slideInStoryboard = (Storyboard)Resources["FloatingNotificationSlideInStoryboard"];
        _slideOutStoryboard = (Storyboard)Resources["FloatingNotificationSlideOutStoryboard"];
        _countdownStoryboard = (Storyboard)Resources["FloatingNotificationCountdownStoryboard"];
        _slideOutStoryboard.Completed += OnHideAnimationCompleted;
        SourceInitialized += (_, _) => WindowCaptionThemeHelper.Apply(this);
        DataContext = ViewModel;
        ViewModel.PropertyChanged += ViewModel_PropertyChanged;
        Loaded += MainWindow_Loaded;
    }

    private async void MainWindow_Loaded(object sender, RoutedEventArgs e)
    {
        if (_settings.CheckForUpdatesOnStartup)
        {
            await CheckForUpdateAsync().ConfigureAwait(false);
        }
    }

    private async Task CheckForUpdateAsync()
    {
        var update = await _updateWorkflow.CheckForUpdateSafeAsync().ConfigureAwait(false);
        Dispatcher.Invoke(() => ApplyUpdateAvailability(update));
    }

    private void ApplyUpdateAvailability(AppUpdateInfo? update)
    {
        _pendingUpdate = update;

        if (update is null)
        {
            ViewModel.UpdateBanner.Message = string.Empty;
            ViewModel.UpdateBanner.BannerVisibility = Visibility.Collapsed;
            ViewModel.UpdateBanner.HeaderButtonVisibility = Visibility.Collapsed;
            return;
        }

        ViewModel.UpdateBanner.Message = string.Format(Strings.UpdateMessage, update.NewVersion, update.CurrentVersion);
        ViewModel.UpdateBanner.BannerVisibility = Visibility.Visible;
        ViewModel.UpdateBanner.HeaderButtonVisibility = Visibility.Visible;
    }

    private void ViewModel_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(MainWindowViewModel.SelectedCategory))
        {
            ScrollLaunchListToTop();
        }
    }

    private void ScrollLaunchListToTop()
    {
        if (FindVisualChild<ScrollViewer>(LaunchListBox) is ScrollViewer scrollViewer)
        {
            scrollViewer.ScrollToTop();
        }
    }

    // ── Settings ────────────────────────────────────────────

    private async void SettingsButton_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new SettingsWindow(this);
        dialog.ShowDialog();
        if (!dialog.SettingsChanged)
        {
            return;
        }

        var settings = dialog.SavedSettings ?? AppSettings.Load();
        _settings = settings;
        if (!settings.DebugUpdate)
        {
            ApplyUpdateAvailability(null);
        }

        // Re-create update service with new settings and re-check.
        _updateWorkflow.SetUpdateService(new GitHubAppUpdateService());
        await CheckForUpdateAsync().ConfigureAwait(false);
    }

    // ── Button click handlers ───────────────────────────────

    private void LaunchItemButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not FrameworkElement { Tag: LaunchItemViewModel item })
        {
            return;
        }

        var result = _itemLaunchService.TryLaunch(item);
        if (!result.IsSuccess)
        {
            ShowFloatingNotification(result.Message, result.Icon);
            return;
        }

        HideFloatingNotification();

        if (_settings.CloseOnLaunch)
        {
            Application.Current.Shutdown();
        }
    }

    private void DeleteButton_Click(object sender, RoutedEventArgs e)
    {
        e.Handled = true;
        if (sender is not FrameworkElement { Tag: LaunchItemViewModel item })
        {
            return;
        }

        ViewModel.RemoveItem(item);
    }

    private void QuickAddButton_Click(object sender, RoutedEventArgs e)
        => ViewModel.TryAddQuickItem();

    private async void UpdateButton_Click(object sender, RoutedEventArgs e)
    {
        if (_pendingUpdate is null)
        {
            return;
        }

        var result = await _updateWorkflow.ApplyUpdateSafeAsync(_pendingUpdate).ConfigureAwait(false);
        if (result.IsSuccess)
        {
            Dispatcher.Invoke(() => Application.Current.Shutdown());
            return;
        }

        Dispatcher.Invoke(() =>
            ShowFloatingNotification(string.Format(Strings.UpdateFailed, result.ErrorMessage), MessageBoxImage.Error));
    }

    private void DismissUpdateButton_Click(object sender, RoutedEventArgs e)
    {
        ViewModel.UpdateBanner.BannerVisibility = Visibility.Collapsed;
    }

    private void FloatingNotificationTimer_Tick(object? sender, EventArgs e)
    {
        HideFloatingNotification();
    }

    private void ShowFloatingNotification(string message, MessageBoxImage icon)
    {
        ViewModel.FloatingNotification.Message = message;
        ViewModel.FloatingNotification.IconType = FloatingNotificationCoordinator.MapIcon(icon);
        FloatingNotificationBanner.Visibility = Visibility.Visible;
        _floatingNotificationCoordinator.BeginShow();
        _slideInStoryboard.Begin(this, HandoffBehavior.SnapshotAndReplace, isControllable: true);
        _countdownStoryboard.Begin(this, HandoffBehavior.SnapshotAndReplace, isControllable: true);
        _floatingNotificationTimer.Stop();
        _floatingNotificationTimer.Start();
    }

    private void HideFloatingNotification()
    {
        var shouldAnimateHide = _floatingNotificationCoordinator.BeginHide(
            FloatingNotificationBanner.Visibility == Visibility.Visible);
        var frozenProgressScale = FloatingNotificationProgressState.CaptureVisibleScale(FloatingNotificationProgressScale.ScaleX);
        _floatingNotificationTimer.Stop();
        _countdownStoryboard.Stop(this);
        FloatingNotificationProgressScale.ScaleX = frozenProgressScale;
        if (!shouldAnimateHide)
        {
            ClearFloatingNotification();
            return;
        }

        _slideOutStoryboard.Begin(this, HandoffBehavior.SnapshotAndReplace, isControllable: true);
    }

    private void OnHideAnimationCompleted(object? sender, EventArgs e)
    {
        if (!_floatingNotificationCoordinator.CanCompleteHide())
        {
            return;
        }

        FloatingNotificationBanner.Visibility = Visibility.Collapsed;
        ClearFloatingNotification();
    }

    private void ClearFloatingNotification()
    {
        ViewModel.FloatingNotification.Message = string.Empty;
        ViewModel.FloatingNotification.IconType = NotificationIconType.None;
    }

    // ── Context menu handlers ───────────────────────────────

    private void ContextMenu_Item_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not MenuItem { Tag: string action })
        {
            return;
        }

        switch (action)
        {
            case "Rename":
                _contextMenuHandler.BeginRename(sender);
                break;

            case "EditCategory":
                _contextMenuHandler.EditCategory(
                    sender,
                    ViewModel.CategoryNames,
                    Strings.Prompt_ChangeCategory,
                    ViewModel.UpdateItemCategory);
                break;

            case "EditArguments":
                _contextMenuHandler.EditValue(
                    sender,
                    Strings.Prompt_ChangeArguments,
                    static item => item.Arguments,
                    ViewModel.UpdateItemArguments);
                break;

            case "Delete":
                _contextMenuHandler.Delete(sender, ViewModel.RemoveItem);
                break;
        }
    }

    // ── Inline rename ───────────────────────────────────────

    private void RenameTextBox_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        _inlineRenameHandler.HandleVisibleChanged(sender);
    }

    private void RenameTextBox_KeyDown(object sender, KeyEventArgs e)
    {
        e.Handled = _inlineRenameHandler.HandleKeyDown(sender, e.Key, ViewModel.UpdateItemDisplayName);
    }

    private void RenameTextBox_LostFocus(object sender, RoutedEventArgs e)
    {
        _inlineRenameHandler.HandleLostFocus(sender, ViewModel.UpdateItemDisplayName);
    }

    // ── Drag & drop ─────────────────────────────────────────

    private void LaunchListBox_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        _dragReorderState.DragStartPoint = e.GetPosition(null);
        _dragReorderState.LastDragPreviewIndex = null;

        if (e.OriginalSource is not DependencyObject source)
        {
            _dragReorderState.DraggedItem = null;
            return;
        }

        _dragReorderState.DraggedItem = FindAncestor<ListBoxItem>(source)?.DataContext as LaunchItemViewModel;
    }

    private void LaunchListBox_PreviewMouseMove(object sender, MouseEventArgs e)
    {
        if (e.LeftButton != MouseButtonState.Pressed || _dragReorderState.DraggedItem is null)
        {
            return;
        }

        var currentPos = e.GetPosition(null);
        var diff = _dragReorderState.DragStartPoint - currentPos;

        if (Math.Abs(diff.X) < SystemParameters.MinimumHorizontalDragDistance &&
            Math.Abs(diff.Y) < SystemParameters.MinimumVerticalDragDistance)
        {
            return;
        }

        DragDrop.DoDragDrop(LaunchListBox, _dragReorderState.DraggedItem, DragDropEffects.Move);
        _dragReorderState.Clear();
    }

    private void LaunchListBox_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
    {
        if (sender is not ListBox listBox)
        {
            return;
        }

        if (FindVisualChild<ScrollViewer>(listBox) is not ScrollViewer scrollViewer)
        {
            return;
        }

        var stepCount = Math.Max(1, Math.Abs(e.Delta) / Mouse.MouseWheelDeltaForOneLine);
        for (var i = 0; i < stepCount; i++)
        {
            if (e.Delta > 0)
            {
                scrollViewer.LineUp();
            }
            else if (e.Delta < 0)
            {
                scrollViewer.LineDown();
            }
        }

        e.Handled = true;
    }

    private void LaunchListBox_DragOver(object sender, DragEventArgs e)
    {
        if (sender is not ListBox listBox)
        {
            e.Effects = DragDropEffects.None;
            e.Handled = true;
            return;
        }

        if (!e.Data.GetDataPresent(typeof(LaunchItemViewModel)))
        {
            e.Effects = DragDropEffects.None;
            e.Handled = true;
            return;
        }

        e.Effects = DragDropEffects.Move;

        if (!_dragDropResolver.TryGetDraggedItemData(e.Data, ViewModel.LaunchItems, out _, out var oldIndex))
        {
            e.Handled = true;
            return;
        }

        ApplyDragPreviewMove(listBox, oldIndex, e.GetPosition(listBox));

        e.Handled = true;
    }

    private void LaunchListBox_Drop(object sender, DragEventArgs e)
    {
        CommitDragReorder();
        e.Handled = true;
    }

    private void Window_DragOver(object sender, DragEventArgs e)
    {
        if (!_dragDropResolver.TryGetDraggedItemData(e.Data, ViewModel.LaunchItems, out _, out var oldIndex))
        {
            return;
        }

        var listPosition = e.GetPosition(LaunchListBox);

        e.Effects = DragDropEffects.Move;
        ApplyDragPreviewMove(LaunchListBox, oldIndex, listPosition);
        e.Handled = true;
    }

    private void Window_Drop(object sender, DragEventArgs e)
    {
        CommitDragReorder();
        e.Handled = true;
    }

    private void ApplyDragPreviewMove(ListBox listBox, int oldIndex, Point listPosition)
    {
        var newIndex = _dragDropResolver.GetDropIndex(listBox, ViewModel.LaunchItems, oldIndex, listPosition);
        if (newIndex >= 0 && newIndex != oldIndex && _dragReorderState.LastDragPreviewIndex != newIndex)
        {
            var previousPositions = CaptureItemTopPositions(listBox);
            ViewModel.PreviewMoveItem(oldIndex, newIndex);
            AnimateReorderTransition(listBox, previousPositions);
            _dragReorderState.LastDragPreviewIndex = newIndex;
        }
    }

    private void CommitDragReorder()
    {
        if (_dragReorderState.ConsumeShouldPersistOrder())
        {
            ViewModel.PersistOrderNow();
        }
    }

    private Dictionary<LaunchItemViewModel, double> CaptureItemTopPositions(ListBox listBox)
    {
        var positions = new Dictionary<LaunchItemViewModel, double>();

        foreach (var item in ViewModel.LaunchItems)
        {
            if (listBox.ItemContainerGenerator.ContainerFromItem(item) is not ListBoxItem container)
            {
                continue;
            }

            positions[item] = container.TranslatePoint(new Point(0, 0), listBox).Y;
        }

        return positions;
    }

    private void AnimateReorderTransition(ListBox listBox, IReadOnlyDictionary<LaunchItemViewModel, double> previousPositions)
    {
        Dispatcher.BeginInvoke(() =>
        {
            foreach (var (item, previousTop) in previousPositions)
            {
                if (listBox.ItemContainerGenerator.ContainerFromItem(item) is not ListBoxItem container)
                {
                    continue;
                }

                var currentTop = container.TranslatePoint(new Point(0, 0), listBox).Y;
                var delta = previousTop - currentTop;
                if (Math.Abs(delta) < 0.5)
                {
                    continue;
                }

                var translate = GetOrCreateTranslateTransform(container);
                translate.BeginAnimation(TranslateTransform.YProperty, null);

                var anim = new DoubleAnimation
                {
                    From = delta,
                    To = 0,
                    Duration = TimeSpan.FromMilliseconds(170),
                    EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
                };

                translate.BeginAnimation(TranslateTransform.YProperty, anim, HandoffBehavior.SnapshotAndReplace);
            }
        }, System.Windows.Threading.DispatcherPriority.Loaded);
    }

    // ── Static utilities ────────────────────────────────────

    private static T? FindAncestor<T>(DependencyObject current) where T : DependencyObject
    {
        while (current is not null)
        {
            if (current is T typed)
            {
                return typed;
            }

            current = VisualTreeHelper.GetParent(current);
        }

        return null;
    }

    private static T? FindVisualChild<T>(DependencyObject current) where T : DependencyObject
    {
        var childrenCount = VisualTreeHelper.GetChildrenCount(current);
        for (var i = 0; i < childrenCount; i++)
        {
            var child = VisualTreeHelper.GetChild(current, i);
            if (child is T typed)
            {
                return typed;
            }

            var nested = FindVisualChild<T>(child);
            if (nested is not null)
            {
                return nested;
            }
        }

        return null;
    }

    private static TranslateTransform GetOrCreateTranslateTransform(UIElement element)
    {
        switch (element.RenderTransform)
        {
            case TranslateTransform tt:
                return tt;

            case TransformGroup group:
                {
                    var existing = group.Children.OfType<TranslateTransform>().FirstOrDefault();
                    if (existing is not null)
                    {
                        return existing;
                    }

                    var created = new TranslateTransform();
                    group.Children.Add(created);
                    return created;
                }

            case null:
                {
                    var created = new TranslateTransform();
                    element.RenderTransform = created;
                    return created;
                }

            default:
                {
                    var created = new TranslateTransform();
                    element.RenderTransform = new TransformGroup
                    {
                        Children = { element.RenderTransform, created }
                    };
                    return created;
                }
        }
    }
}