using System.Collections;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Windows;
using System.Windows.Data;
using applanch.Helpers;
using applanch.Infrastructure.Integration;
using applanch.Core.Resolution;
using applanch.Infrastructure.Storage;

namespace applanch.ViewModels;

public sealed class MainWindowViewModel : ObservableObject
{
    private readonly QuickAddWorkflow _quickAddWorkflow;
    private readonly ILauncherStore _launcherStore;
    private readonly ILaunchItemIconProvider _iconProvider;
    private IReadOnlyList<LauncherEntry> _lastPersistedEntries;
    private AppSettings _settings;
    private LaunchItemViewModel? _selectedLaunchItem;
    private bool _refreshingSuggestions;
    private bool _suspendPersistence;
    private string _quickAddNameOrPath = string.Empty;
    private Category _quickAddCategory = Category.Default;
    private string _quickAddArguments = string.Empty;

    public MainWindowViewModel()
        : this(new AppResolverAdapter(), new LauncherStoreAdapter(), AppSettingsProvider.Current)
    {
    }

    internal MainWindowViewModel(IAppResolver appResolver, ILauncherStore launcherStore, AppSettings? settings = null, ILaunchItemIconProvider? iconProvider = null)
    {
        _launcherStore = launcherStore;
        _settings = settings ?? new AppSettings();
        _iconProvider = iconProvider ?? LaunchItemIconProvider.Shared;
        _iconProvider.ApplySettings(_settings);
        _quickAddWorkflow = new QuickAddWorkflow(appResolver, _iconProvider);

        var loadedEntries = _launcherStore.LoadAll().ToList();

        LaunchItems = loadedEntries
            .Select(entry => new LaunchItemViewModel(entry.Path, entry.Category, entry.Arguments, entry.DisplayName, _iconProvider))
            .ToObservableCollection();

        _lastPersistedEntries = LaunchItems.Select(ToLauncherEntry).ToList();

        CategoryNames = [];
        FilterCategoryNames = [];
        CategorySidebar = new CategorySidebarViewModel(LaunchItems, CategoryNames, FilterCategoryNames);
        CategorySidebar.PropertyChanged += OnCategorySidebarPropertyChanged;
        QuickAddSuggestions = [];
        QuickAddFeedback = new QuickAddFeedbackState();
        FloatingNotification = new FloatingNotificationState();
        UpdateBanner = new UpdateBannerState();

        foreach (var item in LaunchItems)
        {
            item.PropertyChanged += LaunchItem_PropertyChanged;
        }

        FilteredLaunchItems = CollectionViewSource.GetDefaultView(LaunchItems);
        FilteredLaunchItems.Filter = FilterLaunchItem;

        LaunchItems.CollectionChanged += LaunchItems_CollectionChanged;
        RebuildCategoryLists();
        ApplyLaunchItemSort();
        SelectedLaunchItem = LaunchItems.FirstOrDefault();
        RefreshQuickAddSuggestions();
    }

    public ObservableCollection<LaunchItemViewModel> LaunchItems { get; }

    public ObservableCollection<Category> CategoryNames { get; }

    public ObservableCollection<Category> FilterCategoryNames { get; }

    public ObservableCollection<string> QuickAddSuggestions { get; }

    public QuickAddFeedbackState QuickAddFeedback { get; }

    public FloatingNotificationState FloatingNotification { get; }

    public UpdateBannerState UpdateBanner { get; }

    public ICollectionView FilteredLaunchItems { get; }

    public LaunchItemViewModel? SelectedLaunchItem
    {
        get => _selectedLaunchItem;
        set => SetSelectedLaunchItem(value);
    }

    public Visibility SelectedLaunchItemVisibility => SelectedLaunchItem is null ? Visibility.Collapsed : Visibility.Visible;

    public bool IsLaunchItemIconOnlyMode => _settings.LaunchItemIconOnlyMode;

    public string QuickAddNameOrPath
    {
        get => _quickAddNameOrPath;
        set
        {
            if (_refreshingSuggestions)
            {
                return;
            }

            if (!SetField(ref _quickAddNameOrPath, value))
            {
                return;
            }

            RefreshQuickAddSuggestions();
        }
    }

    public Category QuickAddCategory
    {
        get => _quickAddCategory;
        set => SetField(ref _quickAddCategory, value);
    }

    public string QuickAddArguments
    {
        get => _quickAddArguments;
        set => SetField(ref _quickAddArguments, value);
    }

    public Category SelectedCategory
    {
        get => CategorySidebar.SelectedCategory;
        set => CategorySidebar.SelectedCategory = value;
    }

    internal CategorySidebarViewModel CategorySidebar { get; }

    public Visibility EmptyMessageVisibility => FilteredLaunchItems.IsEmpty ? Visibility.Visible : Visibility.Collapsed;

    public QuickAddResult TryAddQuickItem()
    {
        var result = _quickAddWorkflow.TryCreateLaunchItem(
            QuickAddNameOrPath,
            QuickAddCategory,
            QuickAddArguments,
            LaunchItems,
            out var newItem);

        if (!result.IsSuccess)
        {
            return Fail(result.Message, result.Severity);
        }

        LaunchItems.Add(newItem!);
        ResetQuickAddFieldsAfterAdd();
        QuickAddFeedback.Message = string.Empty;
        return QuickAddResult.Success();
    }

    private QuickAddResult Fail(string message, QuickAddMessageSeverity severity)
    {
        QuickAddFeedback.Severity = severity;
        QuickAddFeedback.Message = message;
        return QuickAddResult.Failed(message, severity);
    }

    public void RemoveItem(LaunchItemViewModel item)
    {
        LaunchItems.Remove(item);
    }

    public void InsertItem(LaunchItemViewModel item, int index)
    {
        LaunchItems.Insert(Math.Clamp(index, 0, LaunchItems.Count), item);
    }

    internal void UpdateItemCategory(LaunchItemViewModel item, Category category)
    {
        item.Category = category;
    }

    public void UpdateItemArguments(LaunchItemViewModel item, string newArguments)
    {
        item.Arguments = newArguments;
    }

    public void UpdateItemDisplayName(LaunchItemViewModel item, string newName)
    {
        item.DisplayName = newName;
    }

    public void PreviewMoveItem(int oldIndex, int newIndex)
    {
        if (oldIndex == newIndex)
        {
            return;
        }

        if (oldIndex < 0 || oldIndex >= LaunchItems.Count ||
            newIndex < 0 || newIndex >= LaunchItems.Count)
        {
            return;
        }

        _suspendPersistence = true;
        try
        {
            LaunchItems.Move(oldIndex, newIndex);
        }
        finally
        {
            _suspendPersistence = false;
        }
    }

    public void PersistOrderNow()
    {
        PersistCurrentOrder();
    }

    internal void RefreshLaunchItemPathStates()
    {
        foreach (var item in LaunchItems)
        {
            item.RefreshPathState();
        }
    }

    internal void ApplySettings(AppSettings settings)
    {
        var iconSettingsChanged = _settings.FetchHttpIcons != settings.FetchHttpIcons ||
                                  _settings.AllowPrivateNetworkHttpIconRequests != settings.AllowPrivateNetworkHttpIconRequests;
        var languageChanged = _settings.Language != settings.Language;
        var quickAddSuggestionLimitChanged = _settings.QuickAddSuggestionLimit != settings.QuickAddSuggestionLimit;
        var launchItemIconOnlyModeChanged = _settings.LaunchItemIconOnlyMode != settings.LaunchItemIconOnlyMode;
        _settings = settings;
        _iconProvider.ApplySettings(settings);

        if (languageChanged)
        {
            NormalizeLocalizedDefaultCategories();
            NotifyLocalizedCategoryBindingChanges();
        }

        if (iconSettingsChanged)
        {
            RefreshHttpItemIcons();
        }

        if (quickAddSuggestionLimitChanged)
        {
            RefreshQuickAddSuggestions();
        }

        if (launchItemIconOnlyModeChanged)
        {
            OnPropertyChanged(nameof(IsLaunchItemIconOnlyMode));
        }

        RebuildCategoryLists(forceReplace: languageChanged);
        ApplyLaunchItemSort();
        FilteredLaunchItems.Refresh();
        OnPropertyChanged(nameof(EmptyMessageVisibility));
    }

    private void RefreshHttpItemIcons()
    {
        foreach (var item in LaunchItems)
        {
            if (item.FullPath.IsHttpUrl)
            {
                item.RefreshIcon();
            }
        }
    }

    private void RefreshQuickAddSuggestions()
    {
        _refreshingSuggestions = true;
        try
        {
            var suggestions = _quickAddWorkflow.GetSuggestions(QuickAddNameOrPath, _settings.QuickAddSuggestionLimit);
            ReplaceCollection(QuickAddSuggestions, suggestions);
        }
        finally
        {
            _refreshingSuggestions = false;
        }
    }

    private bool FilterLaunchItem(object item)
    {
        if (item is not LaunchItemViewModel launchItem)
        {
            return false;
        }

        if (CategorySidebar.SelectedCategory.IsAll)
        {
            return true;
        }

        return launchItem.Category == CategorySidebar.SelectedCategory;
    }

    private void PersistCurrentOrder()
    {
        var entries = LaunchItems.Select(ToLauncherEntry).ToList();
        if (_lastPersistedEntries.SequenceEqual(entries))
        {
            return;
        }

        _launcherStore.SaveAll(entries);
        _lastPersistedEntries = entries;
    }

    private void LaunchItems_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        UnsubscribeRemovedItems(e.OldItems);
        SubscribeAddedItems(e.NewItems);

        RebuildCategoryLists();
        FilteredLaunchItems.Refresh();
        EnsureSelectedItem();
        if (!_suspendPersistence)
        {
            PersistCurrentOrder();
        }

        OnPropertyChanged(nameof(EmptyMessageVisibility));
    }

    private void OnCategorySidebarPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName != nameof(CategorySidebarViewModel.SelectedCategory))
        {
            return;
        }

        FilteredLaunchItems.Refresh();
        OnPropertyChanged(nameof(SelectedCategory));
        OnPropertyChanged(nameof(EmptyMessageVisibility));
    }

    private void LaunchItem_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        switch (e.PropertyName)
        {
            case nameof(LaunchItemViewModel.Category):
                RebuildCategoryLists();
                FilteredLaunchItems.Refresh();
                PersistCurrentOrderIfNeeded();

                OnPropertyChanged(nameof(EmptyMessageVisibility));
                return;

            case nameof(LaunchItemViewModel.Arguments):
            case nameof(LaunchItemViewModel.DisplayName):
                PersistCurrentOrderIfNeeded();
                return;

            default:
                return;
        }
    }

    private void NormalizeLocalizedDefaultCategories()
    {
        var categoryUpdated = false;
        _suspendPersistence = true;
        try
        {
            foreach (var item in LaunchItems)
            {
                var category = Category.FromInput(item.Category.Value);
                if (item.Category == category)
                {
                    continue;
                }

                item.Category = category;
                categoryUpdated = true;
            }
        }
        finally
        {
            _suspendPersistence = false;
        }

        if (categoryUpdated)
        {
            PersistCurrentOrder();
        }
    }

    private void NotifyLocalizedCategoryBindingChanges()
    {
        OnPropertyChanged(nameof(CategoryNames));
        OnPropertyChanged(nameof(FilterCategoryNames));
        OnPropertyChanged(nameof(SelectedCategory));
        OnPropertyChanged(nameof(QuickAddCategory));
    }

    private void RebuildCategoryLists(bool forceReplace = false)
    {
        var categories = LaunchCategoryCatalog.BuildCategoryNames(LaunchItems, _settings.CategorySortMode);
        ReplaceCollection(CategoryNames, categories, forceReplace);
        ReplaceCollection(FilterCategoryNames, [Category.All, .. categories], forceReplace);
        EnsureSelectedCategoryIsValid();
    }

    private void EnsureSelectedCategoryIsValid()
    {
        if (FilterCategoryNames.Contains(CategorySidebar.SelectedCategory))
        {
            return;
        }

        CategorySidebar.SelectedCategory = Category.All;
    }

    private void ResetQuickAddFieldsAfterAdd()
    {
        QuickAddNameOrPath = string.Empty;
        QuickAddArguments = string.Empty;
        if (CategorySidebar.SelectedCategory.IsAll)
        {
            QuickAddCategory = Category.Default;
            return;
        }

        QuickAddCategory = CategorySidebar.SelectedCategory;
    }

    private static LauncherEntry ToLauncherEntry(LaunchItemViewModel item) =>
        new(item.FullPath, item.Category, item.Arguments, item.DisplayName);

    private void SetSelectedLaunchItem(LaunchItemViewModel? value)
    {
        if (!SetField(ref _selectedLaunchItem, value))
        {
            return;
        }

        OnPropertyChanged(nameof(SelectedLaunchItemVisibility));
    }

    private void PersistCurrentOrderIfNeeded()
    {
        if (!_suspendPersistence)
        {
            PersistCurrentOrder();
        }
    }

    private void UnsubscribeRemovedItems(IList? oldItems)
    {
        if (oldItems is null)
        {
            return;
        }

        foreach (LaunchItemViewModel item in oldItems)
        {
            item.PropertyChanged -= LaunchItem_PropertyChanged;

            if (ReferenceEquals(item, SelectedLaunchItem))
            {
                SelectedLaunchItem = null;
            }
        }
    }

    private void SubscribeAddedItems(IList? newItems)
    {
        if (newItems is null)
        {
            return;
        }

        foreach (LaunchItemViewModel item in newItems)
        {
            item.PropertyChanged += LaunchItem_PropertyChanged;
        }
    }

    private void ApplyLaunchItemSort()
    {
        using (FilteredLaunchItems.DeferRefresh())
        {
            FilteredLaunchItems.SortDescriptions.Clear();

            switch (_settings.AppListSortMode)
            {
                case AppListSortMode.Name:
                    FilteredLaunchItems.SortDescriptions.Add(new SortDescription(nameof(LaunchItemViewModel.DisplayName), ListSortDirection.Ascending));
                    break;
                case AppListSortMode.CategoryThenName:
                    FilteredLaunchItems.SortDescriptions.Add(new SortDescription(nameof(LaunchItemViewModel.Category), ListSortDirection.Ascending));
                    FilteredLaunchItems.SortDescriptions.Add(new SortDescription(nameof(LaunchItemViewModel.DisplayName), ListSortDirection.Ascending));
                    break;
                case AppListSortMode.Manual:
                default:
                    break;
            }
        }
    }

    private void EnsureSelectedItem() =>
        SelectedLaunchItem ??= FilteredLaunchItems.Cast<LaunchItemViewModel>().FirstOrDefault();

    private static void ReplaceCollection<T>(ObservableCollection<T> target, IEnumerable<T> values, bool forceReplace = false) where T : IEquatable<T>
    {
        if (!forceReplace && target.SequenceEqual(values))
        {
            return;
        }

        target.Clear();
        foreach (var value in values)
        {
            target.Add(value);
        }
    }
}

