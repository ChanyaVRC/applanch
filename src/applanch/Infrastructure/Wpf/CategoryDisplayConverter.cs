using System.Collections.ObjectModel;
using System.Globalization;
using System.Windows.Data;
using applanch.Infrastructure.Storage;

namespace applanch.Infrastructure.Wpf;

[ValueConversion(typeof(Category), typeof(string))]
internal sealed class CategoryDisplayConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value switch
        {
            Category category => category.ToDisplayLabel(),
            IEnumerable<Category> categories => new ObservableCollection<string>(
                categories.Select(c => c.ToDisplayLabel()).ToList()
            ),
            _ => string.Empty
        };
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not string categoryInput)
        {
            return Category.Default;
        }

        return Category.FromInput(categoryInput);
    }
}
