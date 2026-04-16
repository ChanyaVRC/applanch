using applanch.Events;
using applanch.Infrastructure.Storage;
using applanch.Tests.TestSupport;
using Xunit;

namespace applanch.Tests.Infrastructure.Storage;

public class AppSettingsProviderTests
{
    [Fact]
    public void Register_WhenRefreshInvoked_StoresNormalizedCurrentSettings()
    {
        var previousCurrent = AppSettingsProvider.Current;
        var appEvent = AppEventFactory.Create();

        try
        {
            AppSettingsProvider.Register(appEvent);
            var payload = new AppRefreshPayload(
                new AppSettings(),
                new AppSettings
                {
                    ThemeId = "  monochrome  ",
                    QuickAddSuggestionLimit = 0,
                });

            appEvent.Invoke(AppEvents.Refresh, payload);

            Assert.Equal("monochrome", AppSettingsProvider.Current.ThemeId);
            Assert.Equal(AppSettings.MinQuickAddSuggestionLimit, AppSettingsProvider.Current.QuickAddSuggestionLimit);
        }
        finally
        {
            AppSettingsProvider.Unregister(appEvent);
            AppSettingsProvider.ApplyCurrent(previousCurrent);
        }
    }

    [Fact]
    public void Load_WhenAlreadyLoaded_ReturnsCurrent()
    {
        var previousCurrent = AppSettingsProvider.Current;
        var loaded = AppSettingsProvider.ApplyCurrent(new AppSettings { ThemeId = "cached" });

        try
        {
            var result = AppSettingsProvider.Load();

            Assert.Same(loaded, result);
            Assert.Same(AppSettingsProvider.Current, result);
        }
        finally
        {
            AppSettingsProvider.ApplyCurrent(previousCurrent);
        }
    }
}
