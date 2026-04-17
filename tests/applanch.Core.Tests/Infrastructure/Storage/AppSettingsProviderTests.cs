using applanch.Infrastructure.Storage;
using applanch.Settings;
using Xunit;

namespace applanch.Tests.Infrastructure.Storage;

[Collection("SettingsState")]
public class AppSettingsProviderTests
{
    [Fact]
    public void NormalizeAndSetCurrent_StoresNormalizedCurrentSettings()
    {
        var previousCurrent = AppSettingsProvider.Current;

        try
        {
            var settings = new AppSettings
            {
                ThemeId = "  monochrome  ",
                QuickAddSuggestionLimit = 0,
            };

            AppSettingsProvider.NormalizeAndSetCurrent(settings);

            Assert.Equal("monochrome", AppSettingsProvider.Current.ThemeId);
            Assert.Equal(AppSettings.MinQuickAddSuggestionLimit, AppSettingsProvider.Current.QuickAddSuggestionLimit);
        }
        finally
        {
            AppSettingsProvider.NormalizeAndSetCurrent(previousCurrent);
        }
    }

    [Fact]
    public void Current_WhenAlreadyLoaded_ReturnsCachedValue()
    {
        var previousCurrent = AppSettingsProvider.Current;

        try
        {
            var loaded = AppSettingsProvider.NormalizeAndSetCurrent(new AppSettings { ThemeId = "cached" });
            var result = AppSettingsProvider.Current;

            Assert.Equal(loaded, result);
        }
        finally
        {
            AppSettingsProvider.NormalizeAndSetCurrent(previousCurrent);
        }
    }
}