using System.Collections;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Data;
using applanch.Infrastructure.Resolution;
using applanch.Infrastructure.Storage;
using applanch.Properties;

namespace applanch;

public sealed class MainWindowViewModel : INotifyPropertyChanged
{
    private static string AllCategoriesLabel => Resources.AllCategories;
    private const int QuickAddSuggestionsLimit = 10;

    private readonly IAppResolver _appResolver;
    private readonly ILauncherStore _launcherStore;
    private LaunchItemViewModel? _selectedLaunchItem;
    private string _selectedCategory = AllCategoriesLabel;
    private bool _refreshingSuggestions;
    private bool _suspendPersistence;
    private string _quickAddNameOrPath = string.Empty;
    private string _quickAddCategory = LauncherStore.LauncherEntry.DefaultCategory;
    private string _quickAddArguments = string.Empty;

    public MainWindowViewModel()
        : this(new AppResolverAdapter(), new LauncherStoreAdapter())
    {
    }

    internal MainWindowViewModel(IAppResolver appResolver, ILauncherStore launcherStore)
    {
        _appResolver = appResolver;
        _launcherStore = launcherStore;

        LaunchItems = _launcherStore.LoadAll()
            .Select(entry => new LaunchItemViewModel(entry.Path, entry.Category, entry.Arguments, entry.DisplayName))
            .ToObservableCollection();

        CategoryNames = [];
        FilterCategoryNames = [];
        QuickAddSuggestions = [];

        foreach (var item in LaunchItems)
        {
            item.PropertyChanged += LaunchItem_PropertyChanged;
        }

        FilteredLaunchItems = CollectionViewSource.GetDefaultView(LaunchItems);
        FilteredLaunchItems.Filter = FilterLaunchItem;

        LaunchItems.CollectionChanged += LaunchItems_CollectionChanged;
        RebuildCategoryLists();
        SelectedLaunchItem = LaunchItems.FirstOrDefault();
        RefreshQuickAddSuggestions();
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public ObservableCollection<LaunchItemViewModel> LaunchItems { get; }

    public ObservableCollection<string> CategoryNames { get; }

    public ObservableCollection<string> FilterCategoryNames { get; }

    public ObservableCollection<string> QuickAddSuggestions { get; }

    public ICollectionView FilteredLaunchItems { get; }

    public LaunchItemViewModel? SelectedLaunchItem
    {
        get => _selectedLaunchItem;
        set
        {
            if (ReferenceEquals(_selectedLaunchItem, value))
            {
                return;
            }

            _selectedLaunchItem = value;
            OnPropertyChanged(nameof(SelectedLaunchItem));
            OnPropertyChanged(nameof(SelectedLaunchItemVisibility));
        }
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

            if (string.Equals(_quickAddNameOrPath, value, StringComparison.Ordinal))
            {
                return;
            }

            _quickAddNameOrPath = value;
            OnPropertyChanged(nameof(QuickAddNameOrPath));
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
            if (string.Equals(_selectedCategory, value, StringComparison.Ordinal))
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
        var input = QuickAddNameOrPath.Trim();
        if (string.IsNullOrWhiteSpace(input))
        {
            return QuickAddResult.Failed(Resources.Error_QuickAddEmpty, QuickAddMessageSeverity.Information);
        }

        if (!_appResolver.TryResolve(input, out var resolvedApp))
        {
            return QuickAddResult.Failed(string.Format(Resources.Error_QuickAddNotFound, input), QuickAddMessageSeverity.Warning);
        }

        if (LaunchItems.Any(item => string.Equals(item.FullPath, resolvedApp.Path, StringComparison.OrdinalIgnoreCase)))
        {
            return QuickAddResult.Failed(Resources.Error_AlreadyRegistered, QuickAddMessageSeverity.Information);
        }

        var newItem = new LaunchItemViewModel(
            resolvedApp.Path,
            QuickAddCategory,
            QuickAddArguments,
            resolvedApp.DisplayName);

        LaunchItems.Add(newItem);

        ResetQuickAddFieldsAfterAdd();

        return QuickAddResult.Success();
    }

    public void RemoveItem(LaunchItemViewModel item)
    {
        LaunchItems.Remove(item);
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

    private void RefreshQuickAddSuggestions()
    {
        _refreshingSuggestions = true;
        try
        {
            var suggestions = _appResolver.GetSuggestions(QuickAddNameOrPath, QuickAddSuggestionsLimit);
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

        return string.Equals(SelectedCategory, AllCategoriesLabel, StringComparison.Ordinal)
            || string.Equals(launchItem.Category, SelectedCategory, StringComparison.Ordinal);
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
            PersistCurrentOrder();
            OnPropertyChanged(nameof(EmptyMessageVisibility));
            return;
        }

        if (e.PropertyName is nameof(LaunchItemViewModel.Arguments)
                             or nameof(LaunchItemViewModel.DisplayName))
        {
            PersistCurrentOrder();
        }
    }

    private void RebuildCategoryLists()
    {
        var categories = LaunchItems
            .Select(item => item.Category)
            .Where(static category => !string.IsNullOrWhiteSpace(category))
            .Distinct(StringComparer.Ordinal)
            .OrderBy(static category => category, StringComparer.CurrentCulture)
            .ToList();

        ReplaceCollection(CategoryNames, categories);
        ReplaceCollection(FilterCategoryNames, [AllCategoriesLabel, .. categories]);

        if (!FilterCategoryNames.Contains(SelectedCategory))
        {
            _selectedCategory = AllCategoriesLabel;
            OnPropertyChanged(nameof(SelectedCategory));
        }
    }

    private void ResetQuickAddFieldsAfterAdd()
    {
        QuickAddNameOrPath = string.Empty;
        QuickAddArguments = string.Empty;
        QuickAddCategory = IsAllCategorySelected
            ? LauncherStore.LauncherEntry.DefaultCategory
            : SelectedCategory;
    }

    private bool IsAllCategorySelected =>
        string.Equals(SelectedCategory, AllCategoriesLabel, StringComparison.Ordinal);

    private static LauncherStore.LauncherEntry ToLauncherEntry(LaunchItemViewModel item) =>
        new(item.FullPath, item.Category, item.Arguments, item.DisplayName);

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

    private bool SetField(ref string field, string value, [CallerMemberName] string propertyName = "")
    {
        if (string.Equals(field, value, StringComparison.Ordinal))
        {
            return false;
        }

        field = value;
        OnPropertyChanged(propertyName);
        return true;
    }

    private void OnPropertyChanged([CallerMemberName] string propertyName = "") =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}
