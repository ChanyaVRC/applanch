using Xunit;

namespace applanch.Tests;

public class CollectionExtensionsTests
{
    [Fact]
    public void ToObservableCollection_CopiesAllItemsInOrder()
    {
        var source = new[] { "a", "b", "c" };

        var result = source.ToObservableCollection();

        Assert.Equal(source, result);
    }
}
