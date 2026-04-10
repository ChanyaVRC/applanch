using System.Globalization;
using System.Windows.Data;
using applanch.Infrastructure.Storage;

namespace applanch.Infrastructure.Wpf;

[ValueConversion(typeof(Category), typeof(Category))]
internal sealed class CategorySelectionConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value is Category category ? category : Category.All;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        // ListBox can transiently clear selection (null) during ItemsSource refresh.
        return value is Category category ? category : Category.All;
    }
}
