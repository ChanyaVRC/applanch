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
            ResetCurrent(appEvent, previousCurrent);
            AppSettingsProvider.Unregister(appEvent);
        }
    }

    [Fact]
    public void Load_WhenAlreadyLoaded_ReturnsCurrent()
    {
        var previousCurrent = AppSettingsProvider.Current;
        var appEvent = AppEventFactory.Create();

        try
        {
            AppSettingsProvider.Register(appEvent);
            var loaded = UpdateCurrent(appEvent, new AppSettings { ThemeId = "cached" });
            var result = AppSettingsProvider.Load();

            Assert.Equal(loaded, result);
            Assert.Same(AppSettingsProvider.Current, result);
        }
        finally
        {
            ResetCurrent(appEvent, previousCurrent);
            AppSettingsProvider.Unregister(appEvent);
        }
    }

    private static AppSettings UpdateCurrent(AppEvent appEvent, AppSettings settings)
    {
        var normalized = settings.Normalize();
        appEvent.Invoke(AppEvents.Refresh, new AppRefreshPayload(AppSettingsProvider.Current, normalized));
        return normalized;
    }

    private static void ResetCurrent(AppEvent appEvent, AppSettings settings)
    {
        appEvent.Invoke(AppEvents.Refresh, new AppRefreshPayload(AppSettingsProvider.Current, settings));
    }
}
