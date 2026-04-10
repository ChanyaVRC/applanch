using System.Globalization;
using applanch.Infrastructure.Storage;
using applanch.Infrastructure.Wpf;
using Xunit;

namespace applanch.Tests.Infrastructure.Wpf;

public sealed class CategorySelectionConverterTests
{
    private static readonly CultureInfo Culture = CultureInfo.InvariantCulture;

    [Fact]
    public void Convert_Null_ReturnsAllCategory()
    {
        var converter = new CategorySelectionConverter();

        var converted = converter.Convert(null, typeof(Category), null, Culture);

        Assert.Equal(Category.All, Assert.IsType<Category>(converted));
    }

    [Fact]
    public void ConvertBack_Null_ReturnsAllCategory()
    {
        var converter = new CategorySelectionConverter();

        var converted = converter.ConvertBack(null, typeof(Category), null, Culture);

        Assert.Equal(Category.All, Assert.IsType<Category>(converted));
    }

    [Fact]
    public void ConvertBack_Category_ReturnsSameCategory()
    {
        var converter = new CategorySelectionConverter();
        var dev = Category.FromInput("Dev");

        var converted = converter.ConvertBack(dev, typeof(Category), null, Culture);

        Assert.Equal(dev, Assert.IsType<Category>(converted));
    }
}
