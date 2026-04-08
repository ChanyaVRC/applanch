using System.Diagnostics;
using System.Windows;
using applanch.Events;
using applanch.Infrastructure.Storage;
using applanch.Infrastructure.Utilities;
using applanch.ViewModels;
using ListBoxItem = System.Windows.Controls.ListBoxItem;
using Strings = applanch.Properties.Resources;

namespace applanch;

public sealed partial class MainWindow
{
    private void CategorySidebarHoverZone_DragOver(object sender, DragEventArgs e)
    {
        e.Effects = DragDropEffects.None;
        e.Handled = true;
    }

    private void CategorySidebarHoverZone_DragLeave(object sender, DragEventArgs e)
    {
        CategorySidebar.HandleTriggerExited();

        // When dragging a LaunchItem, the cursor may be transitioning into CategorySidebarContainer.
        // DragOver(Container) fires after this DragLeave and calls HandleSidebarEntered(), so skip
        // collapsing here. For non-LaunchItem drags (e.g. external files), collapse normally.
        if (GetDraggedItem(e.Data) is null)
        {
            CategorySidebar.TryCollapseSidebar();
        }

        e.Handled = true;
    }

    private void CategorySidebarContainer_DragOver(object sender, DragEventArgs e)
    {
        e.Effects = DragDropEffects.None;

        if (CategorySidebar.IsCreateDropTargetDescendant(e.OriginalSource))
        {
            e.Effects = GetCategoryCreateDropEffect(e.Data);
            e.Handled = true;
            return;
        }

        e.Effects = GetCategorySidebarDropEffect(e.Data, e.OriginalSource);
        e.Handled = true;
    }

    private void CategorySidebarContainer_DragLeave(object sender, DragEventArgs e)
    {
        HandleCategorySidebarContainerDragLeave();
        e.Handled = true;
    }

    private void CategorySidebarContainer_Drop(object sender, DragEventArgs e)
    {
        try
        {
            ApplyCategoryDrop(e.Data, e.OriginalSource);
            e.Effects = DragDropEffects.Move;
        }
        catch (Exception ex)
        {
            AppLogger.Instance.Warn($"Category drop failed: {ex.Message}");
            e.Effects = DragDropEffects.None;
        }

        ClearCategoryDragTargets();
        e.Handled = true;
    }

    private void CategorySidebarCreateDropTarget_DragOver(object sender, DragEventArgs e)
    {
        e.Effects = GetCategoryCreateDropEffect(e.Data);
        e.Handled = true;
    }

    private void CategorySidebarCreateDropTarget_DragLeave(object sender, DragEventArgs e)
    {
        HandleCategoryCreateDropTargetDragLeave();
        e.Handled = true;
    }

    private void CategorySidebarCreateDropTarget_Drop(object sender, DragEventArgs e)
    {
        try
        {
            ApplyCategoryCreateDrop(e.Data);
            e.Effects = DragDropEffects.Move;
        }
        catch (OperationCanceledException)
        {
            e.Effects = DragDropEffects.None;
        }
        catch (Exception ex)
        {
            AppLogger.Instance.Warn($"Category create drop failed: {ex.Message}");
            e.Effects = DragDropEffects.None;
        }

        ClearCategoryDragTargets();
        e.Handled = true;
    }

    private void CategorySidebarPinToggleButton_Click(object sender, RoutedEventArgs e)
    {
        UpdateCategorySidebarPinned(CategorySidebar.IsPinned);
    }

    private void ApplyCategorySidebarPinnedSetting(bool isPinned, bool animate)
    {
        CategorySidebar.SetPinned(isPinned, animate);
    }

    private void UpdateCategorySidebarPinned(bool isPinned)
    {
        CategorySidebar.SetPinned(isPinned, animate: true);

        if (_settings.CategorySidebarPinned == isPinned)
        {
            return;
        }

        var updatedSettings = SetCategorySidebarPinned(_settings, isPinned);

        if (_appEvent is not null)
        {
            _appEvent.Invoke(AppEvents.Commit, updatedSettings);
            return;
        }

        updatedSettings.Save();
        ApplySettingsFromAppRefresh(updatedSettings);
    }

    internal DragDropEffects GetCategorySidebarDropEffect(IDataObject data, object? originalSource)
    {
        var draggedItem = GetDraggedItem(data);
        if (draggedItem is null)
        {
            ClearCategoryDragTargets();
            return DragDropEffects.None;
        }

        ActivateCategoryDragUi(isCreateDropTargetActive: false, clearCategoryHighlight: false);

        var targetCategory = ResolveCategoryDropTargetOrSelectedCategory(originalSource);
        if (targetCategory is null)
        {
            ClearCategoryDropHighlight();
            return DragDropEffects.None;
        }

        var category = Category.FromInput(targetCategory);

        var effect = DragDropEffects.None;
        if (CanApplyCategoryDrop(draggedItem, category))
        {
            effect = DragDropEffects.Move;
        }

        ListBoxItem? highlightTarget = null;
        if (effect == DragDropEffects.Move)
        {
            highlightTarget = ResolveCategoryDropTargetContainer(originalSource)
                ?? ResolveCategoryDropTargetContainerByCategory(category.ToDisplayLabel());
        }

        UpdateCategoryDropHighlight(highlightTarget);

        return effect;
    }

    internal void HandleCategorySidebarContainerDragLeave()
    {
        if (_isLaunchItemCategoryDragSessionActive)
        {
            CategorySidebar.SetCreateDropTargetVisible(true);
            return;
        }

        ClearCategoryDragTargets();
        CategorySidebar.HandleSidebarExited();
        CategorySidebar.TryCollapseSidebar();
    }

    internal DragDropEffects GetCategoryCreateDropEffect(IDataObject data)
    {
        if (GetDraggedItem(data) is null)
        {
            AppLogger.Instance.Debug("Category DnD state: create target rejected drag (no dragged item data).");
            CategorySidebar.SetCreateDropTargetVisible(false);
            return DragDropEffects.None;
        }

        if (!CategorySidebar.IsCreateDropTargetActive)
        {
            AppLogger.Instance.Debug("Category DnD state: create target activated.");
        }

        ActivateCategoryDragUi(isCreateDropTargetActive: true, clearCategoryHighlight: true);
        return DragDropEffects.Move;
    }

    internal void HandleCategoryCreateDropTargetDragLeave()
    {
        CategorySidebar.SetCreateDropTargetActive(false);
    }

    internal void ApplyCategoryCreateDrop(IDataObject data)
    {
        var draggedItem = GetDraggedItem(data);
        Debug.Assert(
            draggedItem is not null,
            "Create-drop should only run after drag-over accepted a launch item.");

        var targetCategory = _interactionService.PromptWithSuggestions(
            Strings.Prompt_CreateCategory,
            string.Empty,
            ViewModel.CategoryNames,
            this);

        if (string.IsNullOrWhiteSpace(targetCategory))
        {
            throw new OperationCanceledException("Category create drop canceled: no category was provided.");
        }

        MoveItemToCategory(draggedItem!, Category.FromInput(targetCategory));
    }

    internal void ApplyCategoryDrop(IDataObject data, object? originalSource)
    {
        var targetCategory = ResolveCategoryDropTargetOrSelectedCategory(originalSource);
        Debug.Assert(
            targetCategory is not null,
            "Category-drop should only run after drag-over resolved a target category.");

        var draggedItem = GetDraggedItem(data);
        Debug.Assert(
            draggedItem is not null,
            "Category-drop should only run after drag-over accepted a launch item.");

        MoveItemToCategory(draggedItem!, Category.FromInput(targetCategory));
    }

    internal void MoveItemToCategory(LaunchItemViewModel item, Category targetCategory)
    {
        ArgumentNullException.ThrowIfNull(item);

        var previousCategory = item.Category;
        if (!ViewModel.TryMoveItemToCategory(item, targetCategory))
        {
            throw new InvalidOperationException(
                $"Category move failed for '{item.DisplayName}': '{previousCategory.Value}' -> '{targetCategory.Value}'.");
        }

        ShowFloatingNotification(
            string.Format(Strings.Notification_ItemCategoryChanged, item.DisplayName, previousCategory.Value, item.Category.Value),
            MessageBoxImage.Information);
    }

    internal static string? ResolveCategoryDropTarget(object? originalSource)
    {
        if (ResolveCategoryDropTargetContainer(originalSource) is not { DataContext: string category })
        {
            return null;
        }

        if (Category.FromInput(category).IsAll)
        {
            return null;
        }

        return category;
    }

    private static ListBoxItem? ResolveCategoryDropTargetContainer(object? originalSource)
    {
        return originalSource is System.Windows.DependencyObject source
            ? VisualTreeUtilities.FindAncestor<ListBoxItem>(source)
            : null;
    }

    private string? ResolveCategoryDropTargetOrSelectedCategory(object? originalSource)
    {
        var directTarget = ResolveCategoryDropTarget(originalSource);
        if (directTarget is not null)
        {
            return directTarget;
        }

        // Keep blank-space sidebar drop intuitive by treating the selected category as the target.
        var selectedCategory = ViewModel.SelectedCategory;
        if (Category.FromInput(selectedCategory).IsAll)
        {
            return null;
        }

        return selectedCategory;
    }

    private ListBoxItem? ResolveCategoryDropTargetContainerByCategory(string category)
    {
        return CategorySidebar.ResolveCategoryItemContainer(category);
    }

    internal bool IsCategoryDropTargetHighlighted(ListBoxItem item)
    {
        return ReferenceEquals(_highlightedCategoryDropTarget, item) && GetIsCategoryDropTarget(item);
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
        CategorySidebar.SetCreateDropTargetVisible(false);
    }

    private static bool CanApplyCategoryDrop(LaunchItemViewModel draggedItem, Category targetCategory)
    {
        if (targetCategory.IsAll)
        {
            return false;
        }

        return draggedItem.Category != targetCategory;
    }

    private LaunchItemViewModel? GetDraggedItem(IDataObject data)
    {
        if (_dragDropResolver.TryGetDraggedItemData(data, ViewModel.LaunchItems, out var resolvedItem, out _))
        {
            return resolvedItem;
        }

        return null;
    }

    internal void SetLaunchItemCategoryDragSession(bool isActive)
    {
        if (isActive)
        {
            _isLaunchItemCategoryDragSessionActive = true;
            CategorySidebar.SetCreateDropTargetVisible(true);
            return;
        }

        _isLaunchItemCategoryDragSessionActive = false;
        ClearCategoryDragTargets();
        CategorySidebar.HandleSidebarExited();
        CategorySidebar.TryCollapseSidebar();
    }

    private void ActivateCategoryDragUi(bool isCreateDropTargetActive, bool clearCategoryHighlight)
    {
        CategorySidebar.SetCreateDropTargetVisible(true);
        CategorySidebar.SetCreateDropTargetActive(isCreateDropTargetActive);

        if (clearCategoryHighlight)
        {
            ClearCategoryDropHighlight();
        }

        CategorySidebar.HandleSidebarEntered();
    }

}
