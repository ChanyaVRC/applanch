using System.Collections.ObjectModel;
using applanch.Infrastructure.Storage;

namespace applanch.ViewModels;

internal sealed class CategorySidebarViewModel : ObservableObject
{
    private Category _selectedCategory = Category.All;

    internal CategorySidebarViewModel(
        ObservableCollection<LaunchItemViewModel> launchItems,
        ObservableCollection<Category> categoryNames,
        ObservableCollection<Category> filterCategoryNames)
    {
        LaunchItems = launchItems;
        CategoryNames = categoryNames;
        FilterCategoryNames = filterCategoryNames;
    }

    public ObservableCollection<LaunchItemViewModel> LaunchItems { get; }
    public ObservableCollection<Category> CategoryNames { get; }
    public ObservableCollection<Category> FilterCategoryNames { get; }

    public Category SelectedCategory
    {
        get => _selectedCategory;
        set => SetField(ref _selectedCategory, value);
    }

    internal bool CanMoveItemToCategory(LaunchItemViewModel item, Category category) =>
        !category.IsAll && item.Category != category;
}
