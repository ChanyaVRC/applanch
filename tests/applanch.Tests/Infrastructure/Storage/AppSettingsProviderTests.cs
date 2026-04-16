using applanch.Infrastructure.Storage;
using Xunit;

namespace applanch.Tests.Infrastructure.Storage;

public class AppSettingsProviderTests
{
    [Fact]
    public void ApplyCurrent_StoresNormalizedCurrentSettings()
    {
        var previousCurrent = AppSettingsProvider.Current;

        try
        {
            var applied = AppSettingsProvider.ApplyCurrent(new AppSettings
            {
                ThemeId = "  monochrome  ",
                QuickAddSuggestionLimit = 0,
            });

            Assert.Same(applied, AppSettingsProvider.Current);
            Assert.Equal("monochrome", AppSettingsProvider.Current.ThemeId);
            Assert.Equal(AppSettings.MinQuickAddSuggestionLimit, AppSettingsProvider.Current.QuickAddSuggestionLimit);
        }
        finally
        {
            AppSettingsProvider.ApplyCurrent(previousCurrent);
        }
    }
}
