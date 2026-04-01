using System.Globalization;
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
        var previousUi = CultureInfo.CurrentUICulture;
        var previousCulture = CultureInfo.CurrentCulture;

        try
        {
            var culture = new CultureInfo(cultureName);
            CultureInfo.CurrentUICulture = culture;
            CultureInfo.CurrentCulture = culture;

            var value = LocalizedStrings.Instance[nameof(global::applanch.Properties.Resources.DefaultCategory)];

            Assert.Equal(expected, value);
        }
        finally
        {
            CultureInfo.CurrentUICulture = previousUi;
            CultureInfo.CurrentCulture = previousCulture;
        }
    }
}
