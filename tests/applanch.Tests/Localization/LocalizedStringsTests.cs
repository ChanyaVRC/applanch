using applanch.Tests.TestSupport;
using Xunit;

namespace applanch.Tests.Localization;

public class LocalizedStringsTests
{
    [Fact]
    public void NotifyLanguageChanged_RaisesIndexerPropertyChanged()
    {
        string? propertyName = null;
        LocalizedStrings.Instance.PropertyChanged += (_, args) => propertyName = args.PropertyName;

        LocalizedStrings.Instance.NotifyLanguageChanged();

        Assert.Equal("Item[]", propertyName);
    }

    [Theory]
    [InlineData("en", "Uncategorized")]
    [InlineData("ja", "未分類")]
    public void Indexer_ReturnsLocalizedResourceValue(string cultureName, string expected)
    {
        using var cultureScope = new CultureScope(cultureName);

        var value = LocalizedStrings.Instance[nameof(AppResources.DefaultCategory)];

        Assert.Equal(expected, value);
    }
}
