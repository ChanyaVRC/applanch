using System.Collections;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Resources;
using System.Windows;
using System.Windows.Data;
using applanch.Helpers;
using applanch.Infrastructure.Integration;
using applanch.Infrastructure.Resolution;
using applanch.Infrastructure.Storage;
using applanch.Infrastructure.Updates;

namespace applanch.ViewModels;

public sealed class MainWindowViewModel : ObservableObject
{
    private static string AllCategoriesLabel => AppResources.AllCategories;
    private static readonly HashSet<string> KnownAllCategoriesLabels = BuildKnownAllCategoriesLabels();
    private const int QuickAddSuggestionsLimit = 10;

    private readonly QuickAddWorkflow _quickAddWorkflow;
    private readonly ILauncherStore _launcherStore;
    private readonly ILaunchItemIconProvider _iconProvider;
    private AppSettings _settings;
    private LaunchItemViewModel? _selectedLaunchItem;
    private string _selectedCategory = AllCategoriesLabel;
    private bool _refreshingSuggestions;
    private bool _suspendPersistence;
    private string _quickAddNameOrPath = string.Empty;
    private string _quickAddCategory = LauncherEntry.DefaultCategory;
    private string _quickAddArguments = string.Empty;

    public MainWindowViewModel()
        : this(new AppResolverAdapter(), new LauncherStoreAdapter(), AppSettings.Load())
    {
    }

    internal MainWindowViewModel(IAppResolver appResolver, ILauncherStore launcherStore, AppSettings? settings = null, ILaunchItemIconProvider? iconProvider = null)
    {
        _launcherStore = launcherStore;
        _settings = settings ?? new AppSettings();
        _iconProvider = iconProvider ?? LaunchItemIconProvider.Shared;
        _iconProvider.ApplySettings(_settings);
        _quickAddWorkflow = new QuickAddWorkflow(appResolver, _iconProvider);

        LaunchItems = _launcherStore.LoadAll()
            .Select(entry => new LaunchItemViewModel(entry.Path, entry.Category, entry.Arguments, entry.DisplayName, _iconProvider))
            .ToObservableCollection();

        CategoryNames = [];
        FilterCategoryNames = [];
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

    public ObservableCollection<string> CategoryNames { get; }

    public ObservableCollection<string> FilterCategoryNames { get; }

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

    public string QuickAddCategory
    {
        get => _quickAddCategory;
        set => SetField(ref _quickAddCategory, value);
    }

    public string QuickAddArguments
    {
        get => _quickAddArguments;
        set => SetField(ref _quickAddArguments, value);
    }

    public string SelectedCategory
    {
        get => _selectedCategory;
        set
        {
            if (_selectedCategory == value)
            {
                return;
            }

            _selectedCategory = value;
            FilteredLaunchItems.Refresh();
            OnPropertyChanged(nameof(SelectedCategory));
            OnPropertyChanged(nameof(EmptyMessageVisibility));
        }
    }

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

    public void UpdateItemCategory(LaunchItemViewModel item, string newCategory)
    {
        item.Category = newCategory;
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

    internal void ApplySettings(AppSettings settings)
    {
        var iconSettingsChanged = _settings.FetchHttpIcons != settings.FetchHttpIcons ||
                                  _settings.AllowPrivateNetworkHttpIconRequests != settings.AllowPrivateNetworkHttpIconRequests;
        var languageChanged = _settings.Language != settings.Language;
        _settings = settings;
        _iconProvider.ApplySettings(settings);

        if (languageChanged)
        {
            NormalizeLocalizedDefaultCategories();
        }

        if (iconSettingsChanged)
        {
            RefreshHttpItemIcons();
        }

        RebuildCategoryLists();
        ApplyLaunchItemSort();
        RefreshFilteredView();
        OnPropertyChanged(nameof(EmptyMessageVisibility));
    }

    internal void ApplyUpdateAvailability(AppUpdateInfo? update)
    {
        UpdateBanner.ApplyAvailability(update, _settings.UpdateInstallBehavior);
    }

    internal void RevealManualUpdateActions()
    {
        UpdateBanner.RevealManualActions();
    }

    internal void DismissUpdateBanner()
    {
        UpdateBanner.Dismiss();
    }

    internal void ShowFloatingNotification(string message, NotificationIconType iconType, string? actionText = null, Action? action = null)
    {
        FloatingNotification.Show(message, iconType, actionText, action);
    }

    internal void ClearFloatingNotification()
    {
        FloatingNotification.Clear();
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
            var suggestions = _quickAddWorkflow.GetSuggestions(QuickAddNameOrPath, QuickAddSuggestionsLimit);
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

        return IsAllCategoriesLabel(SelectedCategory)
            || launchItem.Category == SelectedCategory;
    }

    private void PersistCurrentOrder()
    {
        _launcherStore.SaveAll(LaunchItems.Select(ToLauncherEntry));
    }

    private void LaunchItems_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        UnsubscribeRemovedItems(e.OldItems);
        SubscribeAddedItems(e.NewItems);

        RebuildCategoryLists();
        RefreshFilteredView();
        EnsureSelectedItem();
        if (!_suspendPersistence)
        {
            PersistCurrentOrder();
        }

        OnPropertyChanged(nameof(EmptyMessageVisibility));
    }

    private void LaunchItem_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(LaunchItemViewModel.Category))
        {
            RebuildCategoryLists();
            RefreshFilteredView();
            PersistCurrentOrderIfNeeded();

            OnPropertyChanged(nameof(EmptyMessageVisibility));
            return;
        }

        if (e.PropertyName is nameof(LaunchItemViewModel.Arguments)
                             or nameof(LaunchItemViewModel.DisplayName))
        {
            PersistCurrentOrderIfNeeded();
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
                var normalizedCategory = LaunchItemNormalization.NormalizeCategory(item.Category);
                if (item.Category == normalizedCategory)
                {
                    continue;
                }

                item.Category = normalizedCategory;
                categoryUpdated = true;
            }
        }
        finally
        {
            _suspendPersistence = false;
        }

        var normalizedQuickAddCategory = LaunchItemNormalization.NormalizeCategory(QuickAddCategory);
        if (_quickAddCategory != normalizedQuickAddCategory)
        {
            _quickAddCategory = normalizedQuickAddCategory;
            OnPropertyChanged(nameof(QuickAddCategory));
        }

        if (categoryUpdated)
        {
            PersistCurrentOrder();
        }
    }

    private void RebuildCategoryLists()
    {
        var categories = BuildCategoryNames();
        ReplaceCollection(CategoryNames, categories);
        ReplaceCollection(FilterCategoryNames, [AllCategoriesLabel, .. categories]);
        EnsureSelectedCategoryIsValid();
    }

    private List<string> BuildCategoryNames()
    {
        var categories = CollectDistinctNonEmptyCategories();
        SortCategoriesIfNeeded(categories);
        MoveDefaultCategoryToLast(categories);
        return categories;
    }

    private void SortCategoriesIfNeeded(List<string> categories)
    {
        if (_settings.CategorySortMode == CategorySortMode.AsAdded)
        {
            return;
        }

        categories.Sort(StringComparer.CurrentCulture);
    }

    private static void MoveDefaultCategoryToLast(List<string> categories)
    {
        var defaultCategory = LauncherEntry.DefaultCategory;
        if (categories.Remove(defaultCategory))
        {
            categories.Add(defaultCategory);
        }
    }

    private void EnsureSelectedCategoryIsValid()
    {
        if (!FilterCategoryNames.Contains(SelectedCategory))
        {
            SelectedCategory = AllCategoriesLabel;
        }
    }

    private List<string> CollectDistinctNonEmptyCategories()
    {
        var seen = new HashSet<string>(StringComparer.Ordinal);
        var categories = new List<string>();

        foreach (var item in LaunchItems)
        {
            if (string.IsNullOrWhiteSpace(item.Category) || !seen.Add(item.Category))
            {
                continue;
            }

            categories.Add(item.Category);
        }

        return categories;
    }

    private static bool IsAllCategoriesLabel(string category) =>
        KnownAllCategoriesLabels.Contains(category);

    private static HashSet<string> BuildKnownAllCategoriesLabels()
    {
        var resourceManager = new ResourceManager(typeof(AppResources).FullName!, typeof(AppResources).Assembly);
        var labels = new HashSet<string>(StringComparer.Ordinal);
        foreach (var culture in LanguageOptionMap.EnumerateSupportedCultures(includeInvariantCulture: true))
        {
            var value = resourceManager.GetString(nameof(AppResources.AllCategories), culture);
            if (!string.IsNullOrWhiteSpace(value))
            {
                labels.Add(value);
            }
        }

        return labels;
    }

    private void ResetQuickAddFieldsAfterAdd()
    {
        QuickAddNameOrPath = string.Empty;
        QuickAddArguments = string.Empty;
        QuickAddCategory = IsAllCategorySelected
            ? LauncherEntry.DefaultCategory
            : SelectedCategory;
    }

    private bool IsAllCategorySelected =>
        SelectedCategory == AllCategoriesLabel;

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

    private void RefreshFilteredView() => FilteredLaunchItems.Refresh();

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

    private static void ReplaceCollection(ObservableCollection<string> target, IEnumerable<string> values)
    {
        if (target.SequenceEqual(values, StringComparer.Ordinal))
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

