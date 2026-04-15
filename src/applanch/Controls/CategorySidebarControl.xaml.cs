using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using applanch.Infrastructure.Dialogs;
using applanch.Infrastructure.Presentation;
using applanch.Infrastructure.Storage;
using applanch.Infrastructure.Utilities;
using applanch.ViewModels;
using Strings = applanch.Properties.Resources;

namespace applanch.Controls;

public sealed partial class CategorySidebarControl : UserControl
{
    private readonly CategorySidebarStateController _stateController = new();
    private bool _isLaunchItemDragSessionActive;
    private bool _suppressPinnedAnimation;
    private ListBoxItem? _highlightedCategoryDropTarget;
    private LaunchListDragDropResolver? _dragDropResolver;
    private IUserInteractionService? _interactionService;
    private CategorySidebarViewModel? _viewModel;

    private LaunchListDragDropResolver DragDropResolver =>
        _dragDropResolver ?? throw new InvalidOperationException("CategorySidebarControl dependencies are not initialized.");

    private IUserInteractionService InteractionService =>
        _interactionService ?? throw new InvalidOperationException("CategorySidebarControl dependencies are not initialized.");

    private CategorySidebarViewModel ViewModel =>
        _viewModel ?? throw new InvalidOperationException("CategorySidebarControl dependencies are not initialized.");

    public CategorySidebarControl()
    {
        InitializeComponent();
    }

    public static readonly DependencyProperty IsSidebarExpandedProperty =
        DependencyProperty.Register(
            nameof(IsSidebarExpanded),
            typeof(bool),
            typeof(CategorySidebarControl),
            new PropertyMetadata(true));

    public bool IsSidebarExpanded
    {
        get => (bool)GetValue(IsSidebarExpandedProperty);
        private set => SetValue(IsSidebarExpandedProperty, value);
    }

    public static readonly DependencyProperty IsCreateDropTargetVisibleProperty =
        DependencyProperty.Register(
            nameof(IsCreateDropTargetVisible),
            typeof(bool),
            typeof(CategorySidebarControl),
            new PropertyMetadata(false));

    public bool IsCreateDropTargetVisible
    {
        get => (bool)GetValue(IsCreateDropTargetVisibleProperty);
        private set => SetValue(IsCreateDropTargetVisibleProperty, value);
    }

    public static readonly DependencyProperty IsCreateDropTargetActiveProperty =
        DependencyProperty.Register(
            nameof(IsCreateDropTargetActive),
            typeof(bool),
            typeof(CategorySidebarControl),
            new PropertyMetadata(false));

    public bool IsCreateDropTargetActive
    {
        get => (bool)GetValue(IsCreateDropTargetActiveProperty);
        private set => SetValue(IsCreateDropTargetActiveProperty, value);
    }

    public static readonly DependencyProperty IsPinnedProperty =
        DependencyProperty.Register(
            nameof(IsPinned),
            typeof(bool),
            typeof(CategorySidebarControl),
            new PropertyMetadata(true, OnIsPinnedChanged));

    public bool IsPinned
    {
        get => (bool)GetValue(IsPinnedProperty);
        set => SetValue(IsPinnedProperty, value);
    }

    public static readonly DependencyProperty IsCategoryDropTargetProperty =
        DependencyProperty.RegisterAttached(
            "IsCategoryDropTarget",
            typeof(bool),
            typeof(CategorySidebarControl),
            new PropertyMetadata(false));

    public static bool GetIsCategoryDropTarget(DependencyObject dependencyObject)
    {
        return (bool)dependencyObject.GetValue(IsCategoryDropTargetProperty);
    }

    public static void SetIsCategoryDropTarget(DependencyObject dependencyObject, bool value)
    {
        dependencyObject.SetValue(IsCategoryDropTargetProperty, value);
    }

    public event RoutedEventHandler? PinToggleClick;

    internal event Action<string, NotificationIconType>? NotificationRequested;

    internal Border SidebarContainerElement => CategorySidebarContainer;

    internal Border HoverZoneElement => CategorySidebarHoverZone;

    internal Border CreateDropTargetElement => CategorySidebarCreateDropTarget;

    internal ToggleButton PinToggleButtonElement => CategorySidebarPinToggleButton;

    public void SetPinned(bool isPinned, bool animate)
    {
        _suppressPinnedAnimation = !animate;
        IsPinned = isPinned;
        _suppressPinnedAnimation = false;
    }

    internal void SetDependencies(
        CategorySidebarViewModel viewModel,
        IUserInteractionService interactionService,
        LaunchListDragDropResolver dragDropResolver)
    {
        ArgumentNullException.ThrowIfNull(viewModel);
        ArgumentNullException.ThrowIfNull(interactionService);
        ArgumentNullException.ThrowIfNull(dragDropResolver);

        _viewModel = viewModel;
        DataContext = viewModel;
        _interactionService = interactionService;
        _dragDropResolver = dragDropResolver;
    }

    public void HandleSidebarEntered()
    {
        ApplyVisualStateIfChanged(_stateController.HandleSidebarEntered());
    }

    public void HandleSidebarExited()
    {
        _stateController.HandleSidebarExited();
    }

    public void HandleTriggerEntered()
    {
        ApplyVisualStateIfChanged(_stateController.HandleTriggerEntered());
    }

    public void HandleTriggerExited()
    {
        _stateController.HandleTriggerExited();
    }

    public void TryCollapseSidebar()
    {
        ApplyVisualStateIfChanged(_stateController.TryCollapse());
    }

    internal void SetLaunchItemCategoryDragSession(bool isActive)
    {
        _isLaunchItemDragSessionActive = isActive;

        if (isActive)
        {
            IsCreateDropTargetVisible = true;
            return;
        }

        ClearCategoryDragTargets();
        HandleSidebarContainerDragLeave();
    }

    public void HandleSidebarContainerDragLeave()
    {
        if (_isLaunchItemDragSessionActive)
        {
            IsCreateDropTargetVisible = true;
            return;
        }

        HandleSidebarExited();
        TryCollapseSidebar();
    }

    internal void HandleCategoryDragLeave()
    {
        ClearCategoryDragTargets();
        HandleSidebarContainerDragLeave();
    }

    public bool IsCreateDropTargetDescendant(DependencyObject? source)
    {
        return source is not null &&
               IsDescendantOf(source, CategorySidebarCreateDropTarget);
    }

    public ListBoxItem? ResolveCategoryItemContainer(Category category)
    {
        return CategorySidebarCategoryList.ItemContainerGenerator.ContainerFromItem(category) as ListBoxItem;
    }

    // ── Category drag-drop behavior ─────────────────────────

    internal DragDropEffects GetCategorySidebarDropEffect(IDataObject data, DependencyObject? originalSource)
    {
        var draggedItem = GetDraggedItem(data);
        if (draggedItem is null)
        {
            ClearCategoryDragTargets();
            return DragDropEffects.None;
        }

        ActivateCategoryDragUi(isCreateDropTargetActive: false, clearCategoryHighlight: false);

        var targetCategory = ResolveCategoryDropTargetOrSelectedCategory(originalSource);
        if (targetCategory is not { } resolvedTargetCategory)
        {
            ClearCategoryDropHighlight();
            return DragDropEffects.None;
        }

        var effect = DragDropEffects.None;
        if (ViewModel.CanMoveItemToCategory(draggedItem, resolvedTargetCategory))
        {
            effect = DragDropEffects.Move;
        }

        ListBoxItem? highlightTarget = null;
        if (effect == DragDropEffects.Move)
        {
            highlightTarget = ResolveCategoryDropTargetContainer(originalSource)
                ?? ResolveCategoryItemContainer(resolvedTargetCategory);
        }

        UpdateCategoryDropHighlight(highlightTarget);

        return effect;
    }

    internal DragDropEffects GetCategoryCreateDropEffect(IDataObject data)
    {
        if (GetDraggedItem(data) is null)
        {
            AppLogger.Instance.Debug("Category DnD state: create target rejected drag (no dragged item data).");
            IsCreateDropTargetVisible = false;
            IsCreateDropTargetActive = false;
            return DragDropEffects.None;
        }

        if (!IsCreateDropTargetActive)
        {
            AppLogger.Instance.Debug("Category DnD state: create target activated.");
        }

        ActivateCategoryDragUi(isCreateDropTargetActive: true, clearCategoryHighlight: true);
        return DragDropEffects.Move;
    }

    internal bool ApplyCategoryCreateDrop(IDataObject data)
    {
        var draggedItem = GetDraggedItem(data);
        Debug.Assert(
            draggedItem is not null,
            "Create-drop should only run after drag-over accepted a launch item.");

        var targetCategory = InteractionService.PromptWithSuggestions(
            Strings.Prompt_CreateCategory,
            Category.Default,
            ViewModel.CategoryNames,
            Window.GetWindow(this));

        if (targetCategory is null || string.IsNullOrWhiteSpace(targetCategory.Value.Text))
        {
            return false;
        }

        MoveItemToCategory(draggedItem, Category.FromInput(targetCategory.Value.Text));
        return true;
    }

    internal bool CanDropToCategorySidebar(IDataObject data, DependencyObject? originalSource)
    {
        var draggedItem = GetDraggedItem(data);
        Debug.Assert(
            draggedItem is not null,
            "Drag-over should only run with valid dragged item data.");

        var targetCategory = ResolveCategoryDropTargetOrSelectedCategory(originalSource);
        return targetCategory is { } resolvedTargetCategory &&
               ViewModel.CanMoveItemToCategory(draggedItem, resolvedTargetCategory);
    }

    internal void ApplyCategoryDrop(IDataObject data, DependencyObject? originalSource)
    {
        var targetCategory = ResolveCategoryDropTargetOrSelectedCategory(originalSource);
        Debug.Assert(
            targetCategory is not null,
            "Category-drop should only run after drag-over resolved a target category.");

        var draggedItem = GetDraggedItem(data);
        Debug.Assert(
            draggedItem is not null,
            "Category-drop should only run after drag-over accepted a launch item.");

        MoveItemToCategory(draggedItem, targetCategory.Value);
    }

    internal void MoveItemToCategory(LaunchItemViewModel item, Category targetCategory)
    {
        ArgumentNullException.ThrowIfNull(item);

        var previousCategory = item.Category;
        Debug.Assert(
            ViewModel.CanMoveItemToCategory(item, targetCategory),
            $"Category move failed for '{item.DisplayName}': '{previousCategory}' -> '{targetCategory}'.");
        item.Category = targetCategory;

        NotificationRequested?.Invoke(
            string.Format(Strings.Notification_ItemCategoryChanged, item.DisplayName, previousCategory, item.Category),
            NotificationIconType.Info);
    }

    internal static Category? ResolveCategoryDropTarget(DependencyObject? originalSource)
    {
        if (ResolveCategoryDropTargetContainer(originalSource) is not { DataContext: Category category })
        {
            return null;
        }

        if (category.IsAll)
        {
            return null;
        }

        return category;
    }

    internal bool IsCategoryDropTargetHighlighted(ListBoxItem item)
    {
        return ReferenceEquals(_highlightedCategoryDropTarget, item) && GetIsCategoryDropTarget(item);
    }

    // ── XAML event handlers ─────────────────────────────────

    private void CategorySidebarContainer_MouseEnter(object sender, MouseEventArgs e)
        => HandleSidebarEntered();

    private void CategorySidebarContainer_MouseLeave(object sender, MouseEventArgs e)
    {
        HandleSidebarExited();
        TryCollapseSidebar();
    }

    private void CategorySidebarContainer_DragEnter(object sender, DragEventArgs e)
        => HandleSidebarEntered();

    private void CategorySidebarContainer_DragOver(object sender, DragEventArgs e)
    {
        e.Effects = DragDropEffects.None;
        var originalSource = e.OriginalSource as DependencyObject;

        if (IsCreateDropTargetDescendant(originalSource))
        {
            e.Effects = GetCategoryCreateDropEffect(e.Data);
            e.Handled = true;
            return;
        }

        e.Effects = GetCategorySidebarDropEffect(e.Data, originalSource);
        e.Handled = true;
    }

    private void CategorySidebarContainer_DragLeave(object sender, DragEventArgs e)
    {
        if (IsPointerWithinSidebarContainer(e))
        {
            e.Handled = true;
            return;
        }

        HandleCategoryDragLeave();
        e.Handled = true;
    }

    private void CategorySidebarContainer_Drop(object sender, DragEventArgs e)
    {
        e.Effects = DragDropEffects.None;
        var originalSource = e.OriginalSource as DependencyObject;
        try
        {
            if (CanDropToCategorySidebar(e.Data, originalSource))
            {
                ApplyCategoryDrop(e.Data, originalSource);
                e.Effects = DragDropEffects.Move;
            }
        }
        catch (Exception ex)
        {
            AppLogger.Instance.Warn(ex, "Category drop failed");
        }

        ClearCategoryDragTargets();
        e.Handled = true;
    }

    private void CategorySidebarHoverZone_MouseEnter(object sender, MouseEventArgs e)
        => HandleTriggerEntered();

    private void CategorySidebarHoverZone_MouseLeave(object sender, MouseEventArgs e)
    {
        HandleTriggerExited();
        TryCollapseSidebar();
    }

    private void CategorySidebarHoverZone_DragOver(object sender, DragEventArgs e)
    {
        HandleTriggerEntered();
        e.Effects = DragDropEffects.None;
        e.Handled = true;
    }

    private void CategorySidebarHoverZone_DragLeave(object sender, DragEventArgs e)
    {
        HandleTriggerExited();

        if (!_isLaunchItemDragSessionActive)
        {
            TryCollapseSidebar();
        }

        e.Handled = true;
    }

    private void CategorySidebarCreateDropTarget_DragOver(object sender, DragEventArgs e)
    {
        e.Effects = GetCategoryCreateDropEffect(e.Data);
        e.Handled = true;
    }

    private void CategorySidebarCreateDropTarget_DragLeave(object sender, DragEventArgs e)
    {
        IsCreateDropTargetActive = false;
        e.Handled = true;
    }

    private void CategorySidebarCreateDropTarget_Drop(object sender, DragEventArgs e)
    {
        try
        {
            e.Effects = ApplyCategoryCreateDrop(e.Data)
                ? DragDropEffects.Move
                : DragDropEffects.None;
        }
        catch (Exception ex)
        {
            AppLogger.Instance.Warn(ex, "Category create drop failed");
            e.Effects = DragDropEffects.None;
        }

        ClearCategoryDragTargets();
        e.Handled = true;
    }

    private void CategorySidebarPinToggleButton_Click(object sender, RoutedEventArgs e)
        => PinToggleClick?.Invoke(sender, e);

    private static void OnIsPinnedChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        Debug.Assert(d is CategorySidebarControl, "OnIsPinnedChanged should be invoked only for CategorySidebarControl instances.");
        var control = (CategorySidebarControl)d;

        var hasStateChanged = control._stateController.SetPinned((bool)e.NewValue);
        if (hasStateChanged)
        {
            control.ApplyVisualState(animate: !control._suppressPinnedAnimation);
        }
    }

    private void ApplyVisualStateIfChanged(bool hasStateChanged)
    {
        if (hasStateChanged)
        {
            ApplyVisualState(animate: true);
        }
    }

    private void ApplyVisualState(bool animate)
    {
        IsSidebarExpanded = _stateController.IsExpanded;
        var targetWidth = _stateController.IsExpanded
            ? MainWindow.CategorySidebarExpandedWidth
            : 0d;

        CategorySidebarContainer.Visibility = Visibility.Visible;

        if (!animate)
        {
            CategorySidebarContainer.BeginAnimation(WidthProperty, null);
            CategorySidebarContainer.Width = targetWidth;
            UpdateSidebarContainerVisibility();
            return;
        }

        var animation = new DoubleAnimation
        {
            To = targetWidth,
            Duration = MainWindow.CategorySidebarAnimationDuration,
            EasingFunction = new CubicEase { EasingMode = EasingMode.EaseInOut },
        };

        animation.Completed += (_, _) => UpdateSidebarContainerVisibility();
        CategorySidebarContainer.BeginAnimation(WidthProperty, animation, HandoffBehavior.SnapshotAndReplace);
    }

    private void UpdateSidebarContainerVisibility()
    {
        CategorySidebarContainer.Visibility = _stateController.IsPinned || _stateController.IsExpanded
            ? Visibility.Visible
            : Visibility.Collapsed;
    }

    // ── Private helpers ─────────────────────────────────────

    private static ListBoxItem? ResolveCategoryDropTargetContainer(DependencyObject? originalSource)
    {
        return originalSource is not null
            ? VisualTreeUtilities.FindAncestor<ListBoxItem>(originalSource)
            : null;
    }

    private Category? ResolveCategoryDropTargetOrSelectedCategory(DependencyObject? originalSource)
    {
        var directTarget = ResolveCategoryDropTarget(originalSource);
        if (directTarget is not null)
        {
            return directTarget;
        }

        var selectedCategory = ViewModel.SelectedCategory;
        if (selectedCategory.IsAll)
        {
            return null;
        }

        return selectedCategory;
    }

    private void UpdateCategoryDropHighlight(ListBoxItem? target)
    {
        var previous = _highlightedCategoryDropTarget;
        if (ReferenceEquals(previous, target))
        {
            return;
        }

        if (previous is not null)
        {
            SetIsCategoryDropTarget(previous, false);
        }

        _highlightedCategoryDropTarget = target;

        if (target is not null)
        {
            SetIsCategoryDropTarget(target, true);
        }
    }

    private void ClearCategoryDropHighlight()
    {
        UpdateCategoryDropHighlight(null);
    }

    private void ClearCategoryDragTargets()
    {
        ClearCategoryDropHighlight();
        IsCreateDropTargetVisible = false;
        IsCreateDropTargetActive = false;
    }

    private LaunchItemViewModel? GetDraggedItem(IDataObject data)
    {
        if (DragDropResolver.TryGetDraggedItemData(data, ViewModel.LaunchItems, out var resolvedItem, out _))
        {
            return resolvedItem;
        }

        return null;
    }

    private void ActivateCategoryDragUi(bool isCreateDropTargetActive, bool clearCategoryHighlight)
    {
        IsCreateDropTargetVisible = true;
        IsCreateDropTargetActive = isCreateDropTargetActive;

        if (clearCategoryHighlight)
        {
            ClearCategoryDropHighlight();
        }

        HandleSidebarEntered();
    }

    private static bool IsDescendantOf(DependencyObject? source, DependencyObject target)
    {
        while (source is not null)
        {
            if (ReferenceEquals(source, target))
            {
                return true;
            }

            source = VisualTreeHelper.GetParent(source);
        }

        return false;
    }

    private bool IsPointerWithinSidebarContainer(DragEventArgs e)
    {
        var position = e.GetPosition(CategorySidebarContainer);
        return position.X >= 0 &&
               position.Y >= 0 &&
               position.X <= CategorySidebarContainer.ActualWidth &&
               position.Y <= CategorySidebarContainer.ActualHeight;
    }
}
