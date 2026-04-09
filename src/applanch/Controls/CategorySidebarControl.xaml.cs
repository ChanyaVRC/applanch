using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using applanch.Infrastructure.Presentation;
using applanch.Infrastructure.Storage;
using applanch.Infrastructure.Utilities;

namespace applanch.Controls;

public sealed partial class CategorySidebarControl : UserControl
{
    private readonly CategorySidebarStateController _stateController = new();
    private bool _suppressPinnedAnimation;

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

    public void SetPinned(bool isPinned, bool animate)
    {
        _suppressPinnedAnimation = !animate;
        IsPinned = isPinned;
        _suppressPinnedAnimation = false;
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

    public void SetCreateDropTargetVisible(bool isVisible)
    {
        IsCreateDropTargetVisible = isVisible;
        if (!isVisible)
        {
            IsCreateDropTargetActive = false;
        }
    }

    public void SetCreateDropTargetActive(bool isActive)
    {
        IsCreateDropTargetActive = isActive;
    }

    public event DragEventHandler? SidebarDragOver;

    public event DragEventHandler? SidebarDragLeave;

    public event DragEventHandler? SidebarDrop;

    public event DragEventHandler? HoverZoneDragOver;

    public event DragEventHandler? HoverZoneDragLeave;

    public event DragEventHandler? CreateDropTargetDragOver;

    public event DragEventHandler? CreateDropTargetDragLeave;

    public event DragEventHandler? CreateDropTargetDrop;

    public event RoutedEventHandler? PinToggleClick;

    internal Border SidebarContainerElement => CategorySidebarContainer;

    internal Border HoverZoneElement => CategorySidebarHoverZone;

    internal Border CreateDropTargetElement => CategorySidebarCreateDropTarget;

    internal ToggleButton PinToggleButtonElement => CategorySidebarPinToggleButton;

    public bool IsCreateDropTargetDescendant(object? source)
    {
        return source is DependencyObject dependencyObject &&
               IsDescendantOf(dependencyObject, CategorySidebarCreateDropTarget);
    }

    public ListBoxItem? ResolveCategoryItemContainer(Category category)
    {
        if (VisualTreeUtilities.FindVisualChild<ListBox>(CategorySidebarContainer) is not { } categoryListBox)
        {
            return null;
        }

        categoryListBox.UpdateLayout();
        return categoryListBox.ItemContainerGenerator.ContainerFromItem(category) as ListBoxItem;
    }

    private void CategorySidebarContainer_MouseEnter(object sender, MouseEventArgs e)
        => HandleSidebarEntered();

    private void CategorySidebarContainer_MouseLeave(object sender, MouseEventArgs e)
    {
        HandleSidebarExited();
        TryCollapseSidebar();
    }

    private void CategorySidebarContainer_DragOver(object sender, DragEventArgs e)
        => SidebarDragOver?.Invoke(sender, e);

    private void CategorySidebarContainer_DragLeave(object sender, DragEventArgs e)
        => SidebarDragLeave?.Invoke(sender, e);

    private void CategorySidebarContainer_Drop(object sender, DragEventArgs e)
        => SidebarDrop?.Invoke(sender, e);

    private void CategorySidebarHoverZone_MouseEnter(object sender, MouseEventArgs e)
        => HandleTriggerEntered();

    private void CategorySidebarHoverZone_MouseLeave(object sender, MouseEventArgs e)
    {
        HandleTriggerExited();
        TryCollapseSidebar();
    }

    private void CategorySidebarHoverZone_DragOver(object sender, DragEventArgs e)
        => HoverZoneDragOver?.Invoke(sender, e);

    private void CategorySidebarHoverZone_DragLeave(object sender, DragEventArgs e)
        => HoverZoneDragLeave?.Invoke(sender, e);

    private void CategorySidebarCreateDropTarget_DragOver(object sender, DragEventArgs e)
        => CreateDropTargetDragOver?.Invoke(sender, e);

    private void CategorySidebarCreateDropTarget_DragLeave(object sender, DragEventArgs e)
        => CreateDropTargetDragLeave?.Invoke(sender, e);

    private void CategorySidebarCreateDropTarget_Drop(object sender, DragEventArgs e)
        => CreateDropTargetDrop?.Invoke(sender, e);

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
}
