using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Threading;
using applanch.Core.Configuration;
using applanch.Events;
using applanch.Infrastructure.Dialogs;
using applanch.Infrastructure.Launch;
using applanch.Infrastructure.Storage;
using applanch.Settings;
using applanch.Infrastructure.Updates;
using applanch.Updates;
using applanch.Theming;
using applanch.ViewModels;
using applanch.Workflows.Items;
using applanch.Workflows.Launch;
using applanch.Core.Utilities;
using applanch.Infrastructure.Wpf;

namespace applanch;

public sealed partial class MainWindow : Window
{
    public const double CategorySidebarExpandedWidth = 172;
    private const double CategorySidebarCollapsedWidth = 0;
    public static readonly Duration CategorySidebarAnimationDuration = new(TimeSpan.FromMilliseconds(220));
    // 188 = CategorySidebarExpandedWidth (172) + 16px gap between sidebar and main content
    public static readonly Thickness CategorySidebarPinnedContentMargin = new(188, 0, 0, 0);
    private const double ReorderDeltaThreshold = 0.5;
    private static readonly Duration ReorderAnimationDuration = new(TimeSpan.FromMilliseconds(170));

    private readonly DragReorderState _dragReorderState = new();
    private readonly IItemLaunchService _itemLaunchService;
    private readonly IUserInteractionService _interactionService;
    private readonly LaunchItemWorkflow _launchItemWorkflow;
    private readonly DeleteItemWorkflow _deleteItemWorkflow;
    private readonly LaunchItemContextMenuHandler _contextMenuHandler;
    private readonly InlineRenameHandler _inlineRenameHandler;
    private readonly LaunchListDragDropResolver _dragDropResolver;
    private readonly UpdateCoordinator _updateCoordinator;
    private AppSettings _settings;
    private SettingsWindow? _settingsWindow;
    private readonly AppEvent _appEvent;
    private readonly Func<AppSettings, IAppUpdateService> _updateServiceFactory;
    private bool _isLaunchListRealizationScheduled;
    private MainWindowViewModel ViewModel { get; }

    internal Controls.CategorySidebarControl CategorySidebarElement => CategorySidebar;

    internal bool IsCategorySidebarExpanded
    {
        get => CategorySidebar.IsSidebarExpanded;
    }

    internal bool IsCategoryCreateDropTargetVisible
    {
        get => CategorySidebar.IsCreateDropTargetVisible;
    }

    internal bool IsCategoryCreateDropTargetActive
    {
        get => CategorySidebar.IsCreateDropTargetActive;
    }

    public MainWindow()
        : this(
            new MainWindowViewModel(),
            new ItemLaunchService(),
            new UserInteractionService(),
            static settings => new GitHubAppUpdateService(settings.DebugUpdate, settings.AllowPrereleaseUpdates),
            AppSettingsProvider.Current)
    {
    }

    internal MainWindow(
        MainWindowViewModel viewModel,
        IItemLaunchService itemLaunchService,
        IUserInteractionService interactionService,
        Func<AppSettings, IAppUpdateService> updateServiceFactory,
        AppSettings settings)
    {
        _settings = settings;
        InitializeComponent();
        ViewModel = viewModel;
        _itemLaunchService = itemLaunchService;
        _interactionService = interactionService;
        _launchItemWorkflow = new LaunchItemWorkflow(_itemLaunchService);
        _deleteItemWorkflow = new DeleteItemWorkflow();
        _contextMenuHandler = new LaunchItemContextMenuHandler(_interactionService, this);
        _inlineRenameHandler = new InlineRenameHandler();
        _dragDropResolver = new LaunchListDragDropResolver();
        _updateServiceFactory = updateServiceFactory;
        _appEvent = AppEvent.Instance;
        _updateCoordinator = new UpdateCoordinator(new UpdateCoordinatorDependencies
        {
            AppEvent = _appEvent,
            UpdateWorkflow = new UpdateWorkflow(_updateServiceFactory(settings)),
            InitialInstallBehavior = settings.UpdateInstallBehavior,
            TryBeginApply = TryBeginUpdateApplyFromUi,
            EndApply = EndUpdateApplyFromUi,
        });
        RegisterCategorySidebarPartNames();
        CategorySidebar.SetDependencies(ViewModel.CategorySidebar, _interactionService, _dragDropResolver);
        CategorySidebar.NotificationRequested += (message, icon) => ShowFloatingNotification(message, icon);
        DataContext = ViewModel;
        ViewModel.PropertyChanged += ViewModel_PropertyChanged;
        _appEvent.Register(AppEvents.Refresh, OnAppRefreshRequested);
        _appEvent.Register(AppEvents.UpdateAvailabilityEvaluated, OnUpdateAvailabilityEvaluated);
        _appEvent.Register(AppEvents.UpdateAutomaticApplyFailed, OnAutomaticApplyFailedForUi);
        _appEvent.Register(AppEvents.UpdateApplyFailed, OnUpdateApplyFailedForUi);
        _appEvent.Register(AppEvents.UpdateApplySucceeded, OnUpdateApplySucceededForUi);
        BundledConfigLoadNotificationCenter.Reported += OnBundledConfigLoadIssueReported;
        ViewModel.ApplySettings(_settings);
        ApplyCategorySidebarPinnedSetting(_settings.CategorySidebarPinned, animate: false);
    }

    private void RegisterCategorySidebarPartNames()
    {
        // Keep MainWindow FindName lookups stable after extracting sidebar XAML into a UserControl.
        RegisterName("CategorySidebarContainer", CategorySidebar.SidebarContainerElement);
        RegisterName("CategorySidebarHoverZone", CategorySidebar.HoverZoneElement);
        RegisterName("CategorySidebarCreateDropTarget", CategorySidebar.CreateDropTargetElement);
        RegisterName("CategorySidebarPinToggleButton", CategorySidebar.PinToggleButtonElement);
    }

    protected override void OnClosed(EventArgs e)
    {
        _appEvent.Unregister(AppEvents.Refresh, OnAppRefreshRequested);
        _appEvent.Unregister(AppEvents.UpdateAvailabilityEvaluated, OnUpdateAvailabilityEvaluated);
        _appEvent.Unregister(AppEvents.UpdateAutomaticApplyFailed, OnAutomaticApplyFailedForUi);
        _appEvent.Unregister(AppEvents.UpdateApplyFailed, OnUpdateApplyFailedForUi);
        _appEvent.Unregister(AppEvents.UpdateApplySucceeded, OnUpdateApplySucceededForUi);
        _updateCoordinator.Dispose();
        BundledConfigLoadNotificationCenter.Reported -= OnBundledConfigLoadIssueReported;

        if (_settingsWindow is { IsLoaded: true })
        {
            _settingsWindow.Closed -= OnSettingsWindowClosed;
            _settingsWindow.Close();
            _settingsWindow = null;
        }

        base.OnClosed(e);
    }

    protected override void OnActivated(EventArgs e)
    {
        base.OnActivated(e);
        ViewModel.RefreshLaunchItemPathStates();
    }

    private void Window_SourceInitialized(object? sender, EventArgs e)
    {
        WindowCaptionThemeHelper.Apply(this);
    }

    private void MainWindow_Loaded(object sender, RoutedEventArgs e)
    {
        ScheduleLaunchListInitialRealization();
        ShowBundledConfigLoadIssues(BundledConfigLoadNotificationCenter.DrainPending());

        if (_settings.CheckForUpdatesOnStartup)
        {
            _appEvent.Invoke(AppEvents.UpdateCheckRequested);
        }
    }

    private void OnBundledConfigLoadIssueReported(BundledConfigLoadIssue issue)
    {
        Dispatcher.InvokeIfRequired(() => ShowBundledConfigLoadIssues([issue]));
    }

    private void OnUpdateAvailabilityEvaluated(UpdateAvailabilityEvaluation availability)
    {
        Dispatcher.InvokeIfRequired(() => ViewModel.UpdateBanner.ApplyAvailability(availability.Update, availability.InstallBehavior));
    }

    private bool TryBeginUpdateApplyFromUi(AppUpdateInfo update)
    {
        var started = false;
        Dispatcher.InvokeIfRequired(() =>
        {
            started = ViewModel.UpdateBanner.TryBeginUpdateApply();
            if (started)
            {
                ShowFloatingNotification(
                    string.Format(AppResources.Notification_InstallingVersion, update.NewVersion),
                    NotificationIconType.Info);
            }
        });
        return started;
    }

    private void EndUpdateApplyFromUi()
    {
        Dispatcher.InvokeIfRequired(ViewModel.UpdateBanner.EndUpdateApply);
    }

    private void OnAutomaticApplyFailedForUi()
    {
        Dispatcher.InvokeIfRequired(ViewModel.UpdateBanner.RevealManualActions);
    }

    private void OnUpdateApplyFailedForUi(UpdateApplyResult result)
    {
        Dispatcher.InvokeIfRequired(() =>
            ShowFloatingNotification(string.Format(AppResources.UpdateFailed, result.ErrorMessage), NotificationIconType.Error));
    }

    private void OnUpdateApplySucceededForUi()
    {
        Dispatcher.InvokeIfRequired(Application.Current.Shutdown);
    }

    internal void ApplySettingsFromAppRefresh(AppSettings settings)
    {
        _settings = settings;

        if (!settings.DebugUpdate)
        {
            _appEvent.Invoke(AppEvents.UpdateAvailabilityChanged, null);
        }
        else
        {
            _appEvent.Invoke(AppEvents.UpdateAvailabilityChanged, ViewModel.UpdateBanner.PendingUpdate);
        }

        ViewModel.ApplySettings(settings);
        ApplyCategorySidebarPinnedSetting(settings.CategorySidebarPinned, animate: false);
    }

    private void OnAppRefreshRequested(AppRefreshPayload payload)
    {
        Dispatcher.InvokeIfRequired(() => ApplySettingsFromAppRefresh(payload.CurrentSettings));
    }

    private void ViewModel_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(MainWindowViewModel.SelectedCategory))
        {
            ScrollLaunchListToTop();
            ScheduleLaunchListInitialRealization();
            return;
        }

        if (e.PropertyName == nameof(MainWindowViewModel.IsLaunchItemIconOnlyMode))
        {
            ScheduleLaunchListInitialRealization();
        }
    }

    private void ScheduleLaunchListInitialRealization()
    {
        if (_isLaunchListRealizationScheduled)
        {
            return;
        }

        _isLaunchListRealizationScheduled = true;
        _ = Dispatcher.BeginInvoke(DispatcherPriority.Render, new Action(() =>
        {
            _isLaunchListRealizationScheduled = false;
            EnsureLaunchListInitialRealization();
        }));
    }

    private void EnsureLaunchListInitialRealization()
    {
        if (!ViewModel.IsLaunchItemIconOnlyMode || LaunchListBox.Items.Count == 0)
        {
            return;
        }

        var panel = VisualTreeUtilities.FindVisualChild<Controls.VirtualizingWrapPanel>(LaunchListBox);
        if (panel is null)
        {
            return;
        }

        panel.SetVerticalOffset(0);

        if (VisualTreeHelper.GetChildrenCount(panel) > 0 && LaunchListBox.ItemContainerGenerator.ContainerFromIndex(0) is not null)
        {
            return;
        }

        panel.InvalidateMeasure();
        panel.InvalidateArrange();
        LaunchListBox.ScrollIntoView(LaunchListBox.Items[0]);
        LaunchListBox.UpdateLayout();
    }

    private void ScrollLaunchListToTop()
    {
        if (VisualTreeUtilities.FindVisualChild<ScrollViewer>(LaunchListBox) is ScrollViewer scrollViewer)
        {
            scrollViewer.ScrollToTop();
        }
    }

    // ── Settings ────────────────────────────────────────────

    private void SettingsButton_Click(object sender, RoutedEventArgs e)
    {
        if (_settingsWindow is { IsLoaded: true })
        {
            EnsureSettingsWindowVisible(_settingsWindow);
            return;
        }

        _settingsWindow = new SettingsWindow(this, _settings, _interactionService, _updateServiceFactory);
        _settingsWindow.Closed += OnSettingsWindowClosed;
        _settingsWindow.Show();
        EnsureSettingsWindowVisible(_settingsWindow);
    }

    private void EnsureSettingsWindowVisible(SettingsWindow window)
    {
        if (window.WindowState == WindowState.Minimized)
        {
            window.WindowState = WindowState.Normal;
        }

        if (!window.IsVisible)
        {
            window.Show();
        }

        window.Owner ??= this;
        window.WindowStartupLocation = WindowStartupLocation.Manual;

        var ownerWidth = ActualWidth > 0 ? ActualWidth : Width;
        var ownerHeight = ActualHeight > 0 ? ActualHeight : Height;
        window.Left = Left + Math.Max((ownerWidth - window.Width) / 2, 0);
        window.Top = Top + Math.Max((ownerHeight - window.Height) / 2, 0);

        window.Activate();
        _ = window.Focus();
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

        _updateCoordinator.Reconfigure(_updateServiceFactory(_settings));
        _appEvent.Invoke(AppEvents.UpdateCheckRequested);
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
                string.Format(AppResources.Confirm_LaunchItem, item.DisplayName),
                AppResources.Confirm_Title,
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
                actionText: ShouldOfferDeleteActionForLaunchFailure(workflowResult.Execution)
                    ? AppResources.Button_DeleteForMissingItem
                    : null,
                action: ShouldOfferDeleteActionForLaunchFailure(workflowResult.Execution)
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

    private void IconModeToggleButton_Click(object sender, RoutedEventArgs e)
    {
        var updatedSettings = ToggleLaunchItemIconOnlyMode(_settings);

        if (_appEvent is not null)
        {
            _appEvent.Invoke(AppEvents.Commit, updatedSettings);
            return;
        }

        updatedSettings.Save();
        ApplySettingsFromAppRefresh(updatedSettings);
    }

    internal static AppSettings ToggleLaunchItemIconOnlyMode(AppSettings settings)
    {
        return settings with
        {
            LaunchItemIconOnlyMode = !settings.LaunchItemIconOnlyMode,
        };
    }

    internal static AppSettings SetCategorySidebarPinned(AppSettings settings, bool isPinned)
    {
        return settings with
        {
            CategorySidebarPinned = isPinned,
        };
    }

    private void DeleteItemWithUndo(LaunchItemViewModel item)
    {
        var workflowResult = _deleteItemWorkflow.TryDelete(
            item,
            _settings,
            () => _interactionService.Confirm(
                string.Format(AppResources.Confirm_DeleteItem, item.DisplayName),
                AppResources.Confirm_Title,
                this),
            ViewModel.LaunchItems,
            ViewModel.RemoveItem);

        if (workflowResult.IsCancelled)
        {
            return;
        }

        ShowFloatingNotification(
            string.Format(AppResources.Notification_ItemDeleted, item.DisplayName),
            NotificationIconType.Info,
            AppResources.Button_Undo,
            () =>
            {
                ViewModel.InsertItem(item, workflowResult.DeletedIndex);
                ShowFloatingNotification(
                    string.Format(AppResources.Notification_ItemRestored, item.DisplayName),
                    NotificationIconType.Info);
            });
    }

    private void QuickAddButton_Click(object sender, RoutedEventArgs e)
        => ViewModel.TryAddQuickItem();

    private void UpdateButton_Click(object sender, RoutedEventArgs e)
    {
        if (ViewModel.UpdateBanner.PendingUpdate is not { } update)
        {
            return;
        }

        _appEvent.Invoke(AppEvents.ApplyUpdateRequested, update);
    }

    private void DismissUpdateButton_Click(object sender, RoutedEventArgs e)
    {
        ViewModel.UpdateBanner.Dismiss();
    }

    private void FloatingNotification_ActionRequested(object sender, RoutedEventArgs e)
    {
        HideFloatingNotification();
        ViewModel.FloatingNotification.Action?.Invoke();
    }

    internal static bool ShouldOfferDeleteActionForLaunchFailure(LaunchExecutionResult execution)
    {
        return !execution.IsSuccess && execution.FailureKind == LaunchFailureKind.MissingTarget;
    }

    private void ShowFloatingNotification(string message, NotificationIconType icon, string? actionText = null, Action? action = null)
    {
        ViewModel.FloatingNotification.Show(message, icon, actionText, action);
        FloatingNotification.ShowNotification();
    }

    private void ShowBundledConfigLoadIssues(IReadOnlyList<BundledConfigLoadIssue> issues)
    {
        if (issues.Count == 0)
        {
            return;
        }

        var message = string.Join(Environment.NewLine, issues.Select(FormatBundledConfigLoadIssue));
        ShowFloatingNotification(message, NotificationIconType.Warning);
    }

    private static string FormatBundledConfigLoadIssue(BundledConfigLoadIssue issue)
    {
        return issue.IsInvalidFormat
            ? string.Format(AppResources.Notification_BundledConfigInvalidFormat, issue.FileName)
            : string.Format(AppResources.Notification_BundledConfigMissing, issue.FileName);
    }

    private void HideFloatingNotification()
    {
        FloatingNotification.HideNotification();
    }

    private void FloatingNotification_Hidden(object sender, RoutedEventArgs e)
    {
        ClearFloatingNotification();
    }

    private void ClearFloatingNotification()
    {
        ViewModel.FloatingNotification.Clear();
    }

    // ── Context menu handlers ───────────────────────────────

    private void ContextMenu_Item_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not MenuItem { Tag: LaunchItemContextMenuAction action })
        {
            return;
        }

        switch (action)
        {
            case LaunchItemContextMenuAction.Rename:
                if (ViewModel.IsLaunchItemIconOnlyMode)
                {
                    _contextMenuHandler.RenameWithPrompt(
                        sender,
                        AppResources.Prompt_ChangeDisplayName,
                        ViewModel.UpdateItemDisplayName);
                }
                else
                {
                    _contextMenuHandler.BeginRename(sender);
                }

                break;

            case LaunchItemContextMenuAction.EditCategory:
                _contextMenuHandler.EditCategory(
                    sender,
                    ViewModel.CategoryNames,
                    AppResources.Prompt_ChangeCategory,
                    CategorySidebar.MoveItemToCategory);
                break;

            case LaunchItemContextMenuAction.EditArguments:
                _contextMenuHandler.EditValue(
                    sender,
                    AppResources.Prompt_ChangeArguments,
                    static item => item.Arguments,
                    ViewModel.UpdateItemArguments);
                break;

            case LaunchItemContextMenuAction.OpenLocation:
                if (LaunchItemContextMenuHandler.GetTargetItem(sender) is { } openLocationTarget)
                {
                    OpenItemLocation(openLocationTarget);
                }

                break;

            case LaunchItemContextMenuAction.Delete:
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
        e.Handled = _inlineRenameHandler.HandleKeyDown(sender, e.Key);
    }

    private void RenameTextBox_LostFocus(object sender, RoutedEventArgs e)
    {
        _inlineRenameHandler.HandleLostFocus(sender);
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

        _dragReorderState.DraggedItem = VisualTreeUtilities.FindAncestor<ListBoxItem>(source)?.DataContext as LaunchItemViewModel;
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

        CategorySidebar.SetLaunchItemCategoryDragSession(isActive: true);

        try
        {
            DragDrop.DoDragDrop(LaunchListBox, _dragReorderState.DraggedItem, DragDropEffects.Move);
        }
        finally
        {
            CategorySidebar.SetLaunchItemCategoryDragSession(isActive: false);
            _dragReorderState.Clear();
        }
    }

    private void LaunchListBox_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
    {
        if (sender is not ListBox listBox)
        {
            return;
        }

        if (VisualTreeUtilities.FindVisualChild<ScrollViewer>(listBox) is not ScrollViewer scrollViewer)
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

    private static void RejectDragDrop(DragEventArgs e)
    {
        e.Effects = DragDropEffects.None;
        e.Handled = true;
    }

    private void ApplyDragPreviewMove(ListBox listBox, int oldIndex, Point listPosition)
    {
        var newIndex = _dragDropResolver.GetDropIndex(listBox, ViewModel.LaunchItems, oldIndex, listPosition, ViewModel.IsLaunchItemIconOnlyMode);
        if (newIndex >= 0 && newIndex != oldIndex && _dragReorderState.LastDragPreviewIndex != newIndex)
        {
            var previousPositions = CaptureItemPositions(listBox);
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

    private Dictionary<LaunchItemViewModel, Point> CaptureItemPositions(ListBox listBox)
    {
        var positions = new Dictionary<LaunchItemViewModel, Point>();

        foreach (var item in ViewModel.LaunchItems)
        {
            if (listBox.ItemContainerGenerator.ContainerFromItem(item) is not ListBoxItem container)
            {
                continue;
            }

            positions[item] = container.TranslatePoint(new Point(0, 0), listBox);
        }

        return positions;
    }

    private void AnimateReorderTransition(ListBox listBox, IReadOnlyDictionary<LaunchItemViewModel, Point> previousPositions)
    {
        Dispatcher.BeginInvoke(() =>
        {
            foreach (var (item, previousPosition) in previousPositions)
            {
                if (listBox.ItemContainerGenerator.ContainerFromItem(item) is not ListBoxItem container)
                {
                    continue;
                }

                var currentPosition = container.TranslatePoint(new Point(0, 0), listBox);
                var deltaX = previousPosition.X - currentPosition.X;
                var deltaY = previousPosition.Y - currentPosition.Y;
                if (!HasSignificantReorderDelta(deltaX, deltaY))
                {
                    continue;
                }

                var translate = EnsureTranslateTransform(container);
                translate.BeginAnimation(TranslateTransform.XProperty, null);
                translate.BeginAnimation(TranslateTransform.YProperty, null);
                BeginReorderAxisAnimation(translate, TranslateTransform.XProperty, deltaX);
                BeginReorderAxisAnimation(translate, TranslateTransform.YProperty, deltaY);
            }
        }, DispatcherPriority.Loaded);
    }

    internal static bool HasSignificantReorderDelta(double deltaX, double deltaY)
    {
        return Math.Abs(deltaX) >= ReorderDeltaThreshold || Math.Abs(deltaY) >= ReorderDeltaThreshold;
    }

    private static void BeginReorderAxisAnimation(TranslateTransform translate, DependencyProperty axisProperty, double delta)
    {
        if (Math.Abs(delta) < ReorderDeltaThreshold)
        {
            return;
        }

        var animation = new DoubleAnimation
        {
            From = delta,
            To = 0,
            Duration = ReorderAnimationDuration,
            EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
        };

        translate.BeginAnimation(axisProperty, animation, HandoffBehavior.SnapshotAndReplace);
    }

    // ── Static utilities ────────────────────────────────────

    internal static TranslateTransform EnsureTranslateTransform(UIElement element)
    {
        return element.RenderTransform switch
        {
            TranslateTransform tt => tt,
            TransformGroup group => GetOrAddTranslateTransform(group),
            null => CreateAndAssignTranslateTransform(element),
            _ => WrapWithTransformGroupAndAppendTranslate(element),
        };
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

    internal static bool TryCreateOpenLocationStartInfo(
        LaunchPath launchPath,
        [NotNullWhen(true)] out ProcessStartInfo? startInfo)
    {
        startInfo = null;
        var path = launchPath.Value;

        if (launchPath.IsUrl || !Path.Exists(path))
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

    internal static bool ShouldOfferDeleteActionForMissingPath(LaunchPath launchPath)
    {
        return !launchPath.IsUrl && !Path.Exists(launchPath.Value);
    }

    private void OpenItemLocation(LaunchItemViewModel item)
    {
        var path = item.FullPath;

        if (!TryCreateOpenLocationStartInfo(path, out var startInfo))
        {
            ShowFloatingNotification(
                string.Format(AppResources.Error_FileNotFound, path.Value),
                NotificationIconType.Warning,
                actionText: ShouldOfferDeleteActionForMissingPath(path)
                    ? AppResources.Button_DeleteForMissingItem
                    : null,
                action: ShouldOfferDeleteActionForMissingPath(path)
                    ? () => DeleteItemWithUndo(item)
                    : null);
            return;
        }

        try
        {
            if (Process.Start(startInfo) is null)
            {
                ShowFloatingNotification(string.Format(AppResources.Error_FileNotFound, path.Value), NotificationIconType.Warning);
            }
        }
        catch (Exception ex)
        {
            AppLogger.Instance.Error(ex, $"Open item location failed for '{path.Value}'");
            ShowFloatingNotification(string.Format(AppResources.Error_FileNotFound, path.Value), NotificationIconType.Warning);
        }
    }
}