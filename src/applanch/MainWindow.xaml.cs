using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Threading;
using applanch.Events;
using applanch.Infrastructure.Dialogs;
using applanch.Infrastructure.Items;
using applanch.Infrastructure.Launch;
using applanch.Infrastructure.Storage;
using applanch.Infrastructure.Theming;
using applanch.Infrastructure.Updates;
using applanch.Infrastructure.Utilities;
using applanch.ViewModels;
using Strings = applanch.Properties.Resources;

namespace applanch;

public sealed partial class MainWindow : Window
{
    private static readonly TimeSpan FloatingNotificationDuration = TimeSpan.FromSeconds(4);
    private readonly DragReorderState _dragReorderState = new();
    private readonly IItemLaunchService _itemLaunchService;
    private readonly IUserInteractionService _interactionService;
    private readonly LaunchItemWorkflow _launchItemWorkflow;
    private readonly DeleteItemWorkflow _deleteItemWorkflow;
    private readonly LaunchItemContextMenuHandler _contextMenuHandler;
    private readonly InlineRenameHandler _inlineRenameHandler;
    private readonly LaunchListDragDropResolver _dragDropResolver;
    private readonly UpdateWorkflow _updateWorkflow;
    private readonly FloatingNotificationCoordinator _floatingNotificationCoordinator;
    private AppSettings _settings;
    private AppUpdateInfo? _pendingUpdate;
    private SettingsWindow? _settingsWindow;
    private readonly DispatcherTimer _floatingNotificationTimer;
    private readonly Storyboard _slideInStoryboard;
    private readonly Storyboard _slideOutStoryboard;
    private readonly Storyboard _countdownStoryboard;
    private readonly AppEvent? _appEvent;
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
        _launchItemWorkflow = new LaunchItemWorkflow(_itemLaunchService);
        _deleteItemWorkflow = new DeleteItemWorkflow();
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
        DataContext = ViewModel;
        ViewModel.PropertyChanged += ViewModel_PropertyChanged;
        _appEvent = (Application.Current as App)?.Events;
        _appEvent?.Register(AppEvents.Refresh, OnAppRefreshRequested);
        _appEvent?.Register(AppEvents.UpdateCheckRequested, OnUpdateCheckRequested);
        _appEvent?.Register(AppEvents.UpdateAvailabilityChanged, OnUpdateAvailabilityChanged);
        ViewModel.ApplySettings(_settings);
    }

    protected override void OnClosed(EventArgs e)
    {
        _appEvent?.Unregister(AppEvents.Refresh, OnAppRefreshRequested);
        _appEvent?.Unregister(AppEvents.UpdateCheckRequested, OnUpdateCheckRequested);
        _appEvent?.Unregister(AppEvents.UpdateAvailabilityChanged, OnUpdateAvailabilityChanged);
        base.OnClosed(e);
    }

    private void Window_SourceInitialized(object? sender, EventArgs e)
    {
        WindowCaptionThemeHelper.Apply(this);
    }

    private void MainWindow_Loaded(object sender, RoutedEventArgs e)
    {
        if (_settings.CheckForUpdatesOnStartup)
        {
            _appEvent?.Invoke(AppEvents.UpdateCheckRequested);
        }
    }

    private async Task RunUpdateCheckAsync()
    {
        var update = await _updateWorkflow.CheckForUpdateSafeAsync().ConfigureAwait(false);
        _appEvent?.Invoke(AppEvents.UpdateAvailabilityChanged, update);
    }

    private void OnUpdateCheckRequested()
    {
        _ = RunUpdateCheckAsync();
    }

    private void OnUpdateAvailabilityChanged(AppUpdateInfo? update)
    {
        if (!Dispatcher.CheckAccess())
        {
            Dispatcher.Invoke(() => ApplyUpdateAvailability(update));
            return;
        }

        ApplyUpdateAvailability(update);
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

    internal void ApplySettingsFromAppRefresh(AppSettings settings)
    {
        _settings = settings;

        if (!settings.DebugUpdate)
        {
            ApplyUpdateAvailability(null);
        }

        ViewModel.ApplySettings(settings);
    }

    private void OnAppRefreshRequested(AppSettings settings)
    {
        if (!Dispatcher.CheckAccess())
        {
            Dispatcher.Invoke(() => ApplySettingsFromAppRefresh(settings));
            return;
        }

        ApplySettingsFromAppRefresh(settings);
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

    private void SettingsButton_Click(object sender, RoutedEventArgs e)
    {
        if (_settingsWindow is { IsLoaded: true })
        {
            _settingsWindow.Activate();
            return;
        }

        _settingsWindow = new SettingsWindow(this);
        _settingsWindow.Closed += OnSettingsWindowClosed;
        _settingsWindow.Show();
    }

    private void OnSettingsWindowClosed(object? sender, EventArgs e)
    {
        _settingsWindow = null;

        // When a language change reloaded the main window, this instance is already
        // closed. The new window handles its own initialization.
        if (!IsLoaded)
        {
            return;
        }

        if (sender is not SettingsWindow { SettingsChanged: true })
        {
            return;
        }

        _updateWorkflow.SetUpdateService(new GitHubAppUpdateService());
        _appEvent?.Invoke(AppEvents.UpdateCheckRequested);
    }

    // -- Button click handlers ---------------------------------------

    private void LaunchItemButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not FrameworkElement { Tag: LaunchItemViewModel item })
        {
            return;
        }

        var workflowResult = _launchItemWorkflow.TryLaunch(
            item,
            _settings,
            () => _interactionService.Confirm(
                string.Format(Strings.Confirm_LaunchItem, item.DisplayName),
                Strings.Confirm_Title,
                this));

        if (workflowResult.IsCancelled)
        {
            return;
        }

        if (!workflowResult.Execution.IsSuccess)
        {
            ShowFloatingNotification(
                workflowResult.Execution.Message,
                workflowResult.Execution.Icon,
                deleteAction: ShouldOfferDeleteActionForLaunchFailure(workflowResult.Execution)
                    ? () => DeleteItemWithUndo(item)
                    : null);
            return;
        }

        HideFloatingNotification();

        switch (workflowResult.PostLaunchBehavior)
        {
            case PostLaunchBehavior.CloseApp:
                Application.Current.Shutdown();
                break;
            case PostLaunchBehavior.MinimizeWindow:
                WindowState = WindowState.Minimized;
                break;
        }
    }

    private void DeleteButton_Click(object sender, RoutedEventArgs e)
    {
        e.Handled = true;
        if (sender is not FrameworkElement { Tag: LaunchItemViewModel item })
        {
            return;
        }

        DeleteItemWithUndo(item);
    }

    private void DeleteItemWithUndo(LaunchItemViewModel item)
    {
        var workflowResult = _deleteItemWorkflow.TryDelete(
            item,
            _settings,
            () => _interactionService.Confirm(
                string.Format(Strings.Confirm_DeleteItem, item.DisplayName),
                Strings.Confirm_Title,
                this),
            ViewModel.LaunchItems,
            ViewModel.RemoveItem);

        if (workflowResult.IsCancelled)
        {
            return;
        }

        ShowFloatingNotification(
            string.Format(Strings.Notification_ItemDeleted, item.DisplayName),
            MessageBoxImage.Information,
            undoAction: () =>
            {
                ViewModel.InsertItem(item, workflowResult.DeletedIndex);
                ShowFloatingNotification(
                    string.Format(Strings.Notification_ItemRestored, item.DisplayName),
                    MessageBoxImage.Information);
            });
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

    private void UndoButton_Click(object sender, RoutedEventArgs e)
    {
        HideFloatingNotification();
        ViewModel.FloatingNotification.UndoAction?.Invoke();
    }

    private void DeleteNotificationButton_Click(object sender, RoutedEventArgs e)
    {
        HideFloatingNotification();
        ViewModel.FloatingNotification.DeleteAction?.Invoke();
    }

    internal static bool ShouldOfferDeleteActionForLaunchFailure(LaunchExecutionResult execution)
    {
        return !execution.IsSuccess && execution.FailureKind == LaunchFailureKind.MissingTarget;
    }

    private void ShowFloatingNotification(string message, MessageBoxImage icon, Action? undoAction = null, Action? deleteAction = null)
    {
        ViewModel.FloatingNotification.Message = message;
        ViewModel.FloatingNotification.IconType = FloatingNotificationCoordinator.MapIcon(icon);
        ViewModel.FloatingNotification.UndoAction = undoAction;
        ViewModel.FloatingNotification.DeleteAction = deleteAction;
        FloatingNotificationBanner.Visibility = Visibility.Visible;
        _floatingNotificationCoordinator.BeginShow();
        _slideInStoryboard.Begin(this, HandoffBehavior.SnapshotAndReplace, isControllable: true);
        _countdownStoryboard.Begin(this, HandoffBehavior.SnapshotAndReplace, isControllable: true);
        _floatingNotificationTimer.Stop();
        _floatingNotificationTimer.Start();
    }

    private void HideFloatingNotification()
    {
        var isBannerVisible = FloatingNotificationBanner.Visibility == Visibility.Visible;
        var shouldAnimateHide = _floatingNotificationCoordinator.BeginHide(isBannerVisible);
        _floatingNotificationTimer.Stop();

        if (isBannerVisible)
        {
            var frozenProgressScale = FloatingNotificationProgressState.CaptureVisibleScale(FloatingNotificationProgressScale.ScaleX);
            _countdownStoryboard.Stop(this);
            FloatingNotificationProgressScale.ScaleX = frozenProgressScale;
        }

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
        ViewModel.FloatingNotification.UndoAction = null;
        ViewModel.FloatingNotification.DeleteAction = null;
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

            case "OpenLocation":
                if (LaunchItemContextMenuHandler.GetTargetItem(sender) is { } openLocationTarget)
                {
                    OpenItemLocation(openLocationTarget);
                }

                break;

            case "Delete":
                if (LaunchItemContextMenuHandler.GetTargetItem(sender) is { } deleteTarget)
                {
                    DeleteItemWithUndo(deleteTarget);
                }

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
        if (_settings.AppListSortMode != AppListSortMode.Manual)
        {
            return;
        }

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
        if (_settings.AppListSortMode != AppListSortMode.Manual)
        {
            return;
        }

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
        if (_settings.AppListSortMode != AppListSortMode.Manual)
        {
            RejectDragDrop(e);
            return;
        }

        if (sender is not ListBox listBox)
        {
            RejectDragDrop(e);
            return;
        }

        if (!e.Data.GetDataPresent(typeof(LaunchItemViewModel)))
        {
            RejectDragDrop(e);
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
        if (_settings.AppListSortMode != AppListSortMode.Manual)
        {
            e.Handled = true;
            return;
        }

        CommitDragReorder();
        e.Handled = true;
    }

    private void Window_DragOver(object sender, DragEventArgs e)
    {
        if (_settings.AppListSortMode != AppListSortMode.Manual)
        {
            return;
        }

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
        if (_settings.AppListSortMode != AppListSortMode.Manual)
        {
            e.Handled = true;
            return;
        }

        CommitDragReorder();
        e.Handled = true;
    }

    private static void RejectDragDrop(DragEventArgs e)
    {
        e.Effects = DragDropEffects.None;
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

                var translate = EnsureTranslateTransform(container);
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

    internal static TranslateTransform EnsureTranslateTransform(UIElement element)
    {
        switch (element.RenderTransform)
        {
            case TranslateTransform tt:
                return tt;

            case TransformGroup group:
                return GetOrAddTranslateTransform(group);

            case null:
                return CreateAndAssignTranslateTransform(element);

            default:
                return WrapWithTransformGroupAndAppendTranslate(element);
        }
    }

    private static TranslateTransform GetOrAddTranslateTransform(TransformGroup group)
    {
        foreach (var transform in group.Children)
        {
            if (transform is TranslateTransform existing)
            {
                return existing;
            }
        }

        var created = new TranslateTransform();
        group.Children.Add(created);
        return created;
    }

    private static TranslateTransform CreateAndAssignTranslateTransform(UIElement element)
    {
        var created = new TranslateTransform();
        element.RenderTransform = created;
        return created;
    }

    private static TranslateTransform WrapWithTransformGroupAndAppendTranslate(UIElement element)
    {
        var created = new TranslateTransform();
        element.RenderTransform = new TransformGroup
        {
            Children = { element.RenderTransform, created }
        };

        return created;
    }

    internal static bool TryCreateOpenLocationStartInfo(string path, out ProcessStartInfo startInfo)
    {
        startInfo = default!;

        if (PathNormalization.IsUrl(path) || !Path.Exists(path))
        {
            return false;
        }

        startInfo = new ProcessStartInfo
        {
            UseShellExecute = true,
            FileName = "explorer.exe",
            Arguments = Directory.Exists(path)
                ? $"\"{path}\""
                : $"/select,\"{path}\"",
        };

        return true;
    }

    internal static bool ShouldOfferDeleteActionForMissingPath(string path)
    {
        return !PathNormalization.IsUrl(path) && !Path.Exists(path);
    }

    private void OpenItemLocation(LaunchItemViewModel item)
    {
        var path = item.FullPath;

        if (!TryCreateOpenLocationStartInfo(path, out var startInfo))
        {
            ShowFloatingNotification(
                string.Format(Strings.Error_FileNotFound, path),
                MessageBoxImage.Warning,
                deleteAction: ShouldOfferDeleteActionForMissingPath(path)
                    ? () => DeleteItemWithUndo(item)
                    : null);
            return;
        }

        try
        {
            if (Process.Start(startInfo) is null)
            {
                ShowFloatingNotification(string.Format(Strings.Error_FileNotFound, path), MessageBoxImage.Warning);
            }
        }
        catch (Exception ex)
        {
            AppLogger.Instance.Warn($"Open item location failed for '{path}': {ex.Message}");
            ShowFloatingNotification(string.Format(Strings.Error_FileNotFound, path), MessageBoxImage.Warning);
        }
    }
}