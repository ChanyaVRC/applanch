using System.Diagnostics.CodeAnalysis;
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
    private Point _dragStartPoint;
    private LaunchItemViewModel? _draggedItem;
    private int? _lastDragPreviewIndex;
    private readonly IItemLaunchService _itemLaunchService;
    private readonly IUserInteractionService _interactionService;
    private IAppUpdateService _updateService;
    private AppSettings _settings;
    private AppUpdateInfo? _pendingUpdate;
    private readonly DispatcherTimer _floatingNotificationTimer;
    private int _floatingNotificationAnimationVersion;
    private int _expectedHideVersion;
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
        _updateService = updateService;
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
            await CheckForUpdateAsync(_updateService).ConfigureAwait(false);
        }
    }

    private async Task CheckForUpdateAsync(IAppUpdateService updateService)
    {
        try
        {
            var update = await updateService.CheckForUpdateAsync().ConfigureAwait(false);
            Dispatcher.Invoke(() => ApplyUpdateAvailability(update));
        }
        catch (Exception ex)
        {
            AppLogger.Instance.Error(ex, "Update check failed");
            Dispatcher.Invoke(() => ApplyUpdateAvailability(null));
        }
    }

    private void ApplyUpdateAvailability(AppUpdateInfo? update)
    {
        _pendingUpdate = update;

        if (update is null)
        {
            UpdateBanner.Visibility = Visibility.Collapsed;
            HeaderUpdateButton.Visibility = Visibility.Collapsed;
            return;
        }

        UpdateMessageText.Text = string.Format(Strings.UpdateMessage, update.NewVersion, update.CurrentVersion);
        UpdateBanner.Visibility = Visibility.Visible;
        HeaderUpdateButton.Visibility = Visibility.Visible;
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
        _updateService = new GitHubAppUpdateService();
        await CheckForUpdateAsync(_updateService).ConfigureAwait(false);
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

        try
        {
            await _updateService.ApplyUpdateAsync(_pendingUpdate).ConfigureAwait(false);
            Dispatcher.Invoke(() => Application.Current.Shutdown());
        }
        catch (Exception ex)
        {
            AppLogger.Instance.Error(ex, "Update apply failed");
            Dispatcher.Invoke(() =>
                ShowFloatingNotification(string.Format(Strings.UpdateFailed, ex.Message), MessageBoxImage.Error));
        }
    }

    private void DismissUpdateButton_Click(object sender, RoutedEventArgs e)
    {
        UpdateBanner.Visibility = Visibility.Collapsed;
    }

    private void FloatingNotificationTimer_Tick(object? sender, EventArgs e)
    {
        HideFloatingNotification();
    }

    private void ShowFloatingNotification(string message, MessageBoxImage icon)
    {
        _floatingNotificationAnimationVersion++;
        ViewModel.FloatingNotification.Message = message;
        ViewModel.FloatingNotification.IconType = ToNotificationIconType(icon);
        FloatingNotificationBanner.Visibility = Visibility.Visible;
        _slideInStoryboard.Begin(this, HandoffBehavior.SnapshotAndReplace, isControllable: true);
        _countdownStoryboard.Begin(this, HandoffBehavior.SnapshotAndReplace, isControllable: true);
        _floatingNotificationTimer.Stop();
        _floatingNotificationTimer.Start();
    }

    private void HideFloatingNotification()
    {
        _expectedHideVersion = ++_floatingNotificationAnimationVersion;
        _floatingNotificationTimer.Stop();
        _countdownStoryboard.Stop(this);

        if (FloatingNotificationBanner.Visibility != Visibility.Visible)
        {
            ClearFloatingNotification();
            return;
        }

        _slideOutStoryboard.Begin(this, HandoffBehavior.SnapshotAndReplace, isControllable: true);
    }

    private void OnHideAnimationCompleted(object? sender, EventArgs e)
    {
        if (_expectedHideVersion != _floatingNotificationAnimationVersion)
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

    private static NotificationIconType ToNotificationIconType(MessageBoxImage icon) => icon switch
    {
        MessageBoxImage.Error => NotificationIconType.Error,
        MessageBoxImage.Warning => NotificationIconType.Warning,
        MessageBoxImage.Information => NotificationIconType.Info,
        _ => NotificationIconType.None,
    };

    // ── Context menu handlers ───────────────────────────────

    private void ContextMenu_EditCategory_Click(object sender, RoutedEventArgs e)
    {
        var item = GetContextMenuTargetItem(sender);
        if (item is null)
        {
            return;
        }

        var suggestions = ViewModel.CategoryNames
            .Where(static name => !string.IsNullOrWhiteSpace(name))
            .Distinct(StringComparer.Ordinal)
            .ToArray();

        var newValue = _interactionService.PromptWithSuggestions(Strings.Prompt_ChangeCategory, item.Category, suggestions, this);
        if (newValue is null)
        {
            return;
        }

        ViewModel.UpdateItemCategory(item, newValue);
    }

    private void ContextMenu_EditArguments_Click(object sender, RoutedEventArgs e)
    {
        EditItemFromContextMenu(sender, Strings.Prompt_ChangeArguments, static item => item.Arguments, ViewModel.UpdateItemArguments);
    }

    private void ContextMenu_RenameItem_Click(object sender, RoutedEventArgs e)
    {
        var item = GetContextMenuTargetItem(sender);
        if (item is null)
        {
            return;
        }

        item.EditingName = item.DisplayName;
        item.IsRenaming = true;
    }

    private void ContextMenu_Delete_Click(object sender, RoutedEventArgs e)
    {
        var item = GetContextMenuTargetItem(sender);
        if (item is null)
        {
            return;
        }

        ViewModel.RemoveItem(item);
    }

    private void EditItemFromContextMenu(object sender, string title, Func<LaunchItemViewModel, string> valueSelector, Action<LaunchItemViewModel, string> applyAction)
    {
        var item = GetContextMenuTargetItem(sender);
        if (item is null)
        {
            return;
        }

        var newValue = _interactionService.Prompt(title, valueSelector(item), this);
        if (newValue is null)
        {
            return;
        }

        applyAction(item, newValue);
    }

    private static LaunchItemViewModel? GetContextMenuTargetItem(object sender)
    {
        if (sender is not MenuItem { Parent: ContextMenu contextMenu })
        {
            return null;
        }

        if (contextMenu.PlacementTarget is not FrameworkElement { DataContext: LaunchItemViewModel item })
        {
            return null;
        }

        return item;
    }

    // ── Inline rename ───────────────────────────────────────

    private void RenameTextBox_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        if (sender is TextBox tb && tb.IsVisible)
        {
            tb.Focus();
            tb.SelectAll();
        }
    }

    private void RenameTextBox_KeyDown(object sender, KeyEventArgs e)
    {
        if (sender is not TextBox tb)
        {
            return;
        }

        if (tb.DataContext is not LaunchItemViewModel item)
        {
            return;
        }

        if (e.Key == Key.Return)
        {
            e.Handled = true;
            CommitRename(item);
        }
        else if (e.Key == Key.Escape)
        {
            e.Handled = true;
            item.IsRenaming = false;
        }
    }

    private void RenameTextBox_LostFocus(object sender, RoutedEventArgs e)
    {
        if (sender is TextBox tb && tb.DataContext is LaunchItemViewModel item && item.IsRenaming)
        {
            CommitRename(item);
        }
    }

    private void CommitRename(LaunchItemViewModel item)
    {
        ViewModel.UpdateItemDisplayName(item, item.EditingName);
        item.IsRenaming = false;
    }

    // ── Drag & drop ─────────────────────────────────────────

    private void LaunchListBox_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        _dragStartPoint = e.GetPosition(null);
        _lastDragPreviewIndex = null;

        if (e.OriginalSource is not DependencyObject source)
        {
            _draggedItem = null;
            return;
        }

        _draggedItem = FindAncestor<ListBoxItem>(source)?.DataContext as LaunchItemViewModel;
    }

    private void LaunchListBox_PreviewMouseMove(object sender, MouseEventArgs e)
    {
        if (e.LeftButton != MouseButtonState.Pressed || _draggedItem is null)
        {
            return;
        }

        var currentPos = e.GetPosition(null);
        var diff = _dragStartPoint - currentPos;

        if (Math.Abs(diff.X) < SystemParameters.MinimumHorizontalDragDistance &&
            Math.Abs(diff.Y) < SystemParameters.MinimumVerticalDragDistance)
        {
            return;
        }

        DragDrop.DoDragDrop(LaunchListBox, _draggedItem, DragDropEffects.Move);
        _draggedItem = null;
        _lastDragPreviewIndex = null;
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

        if (!TryGetDraggedItemData(e.Data, out _, out var oldIndex))
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
        if (!TryGetDraggedItemData(e.Data, out _, out var oldIndex))
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
        var newIndex = GetDropIndex(listBox, oldIndex, listPosition);
        if (newIndex >= 0 && newIndex != oldIndex && _lastDragPreviewIndex != newIndex)
        {
            var previousPositions = CaptureItemTopPositions(listBox);
            ViewModel.PreviewMoveItem(oldIndex, newIndex);
            AnimateReorderTransition(listBox, previousPositions);
            _lastDragPreviewIndex = newIndex;
        }
    }

    private void CommitDragReorder()
    {
        if (_lastDragPreviewIndex.HasValue)
        {
            ViewModel.PersistOrderNow();
        }

        _draggedItem = null;
        _lastDragPreviewIndex = null;
    }

    private int GetDropIndex(ListBox listBox, int oldIndex, Point listPosition)
    {
        var count = ViewModel.LaunchItems.Count;
        if (count <= 1)
        {
            return oldIndex;
        }

        int desiredInsertIndex;

        if (listPosition.Y <= 0)
        {
            desiredInsertIndex = 0;
        }
        else if (listPosition.Y >= listBox.ActualHeight)
        {
            desiredInsertIndex = count;
        }
        else
        {
            var targetContainer = listBox.InputHitTest(listPosition) is DependencyObject hit
                ? FindAncestor<ListBoxItem>(hit)
                : null;
            if (targetContainer?.DataContext is not LaunchItemViewModel targetData)
            {
                desiredInsertIndex = oldIndex;
            }
            else
            {
                var targetIndex = ViewModel.LaunchItems.IndexOf(targetData);
                if (targetIndex < 0)
                {
                    desiredInsertIndex = oldIndex;
                }
                else
                {
                    var containerOrigin = targetContainer.TranslatePoint(new Point(0, 0), listBox);
                    var dropOnItem = new Point(listPosition.X - containerOrigin.X, listPosition.Y - containerOrigin.Y);
                    var insertAfter = dropOnItem.Y > targetContainer.ActualHeight / 2;
                    desiredInsertIndex = insertAfter ? targetIndex + 1 : targetIndex;
                }
            }
        }

        return DragReorderIndexCalculator.Calculate(oldIndex, desiredInsertIndex, count);
    }

    private bool TryGetDraggedItemData(IDataObject data, [NotNullWhen(true)] out LaunchItemViewModel? draggedItem, out int oldIndex)
    {
        draggedItem = null;
        oldIndex = -1;

        if (!data.GetDataPresent(typeof(LaunchItemViewModel)) ||
            data.GetData(typeof(LaunchItemViewModel)) is not LaunchItemViewModel item)
        {
            return false;
        }

        var index = ViewModel.LaunchItems.IndexOf(item);
        if (index < 0)
        {
            return false;
        }

        draggedItem = item;
        oldIndex = index;
        return true;
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