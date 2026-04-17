using applanch.Events;
using applanch.Infrastructure.Storage;
using applanch.Settings;
using applanch.Updates;
using applanch.Tests.TestSupport;
using Xunit;

namespace applanch.Tests.Application;

[Collection("SettingsState")]
public class AppEventTests
{
    [Fact]
    public void InvokeCommit_NotifiesSubscribersWithNormalizedPayload()
    {
        var previousCurrent = AppSettingsProvider.Current;
        var appEvent = AppEventFactory.Create();
        AppSettings? committed = null;
        appEvent.Subscribe(AppEvents.Commit, settings => committed = settings);
        var settings = new AppSettings { ThemeId = "  monochrome  ", QuickAddSuggestionLimit = 0 };

        try
        {
            appEvent.Invoke(AppEvents.Commit, settings);

            Assert.NotNull(committed);
            Assert.Equal("monochrome", committed.ThemeId);
            Assert.Equal(AppSettings.MinQuickAddSuggestionLimit, committed.QuickAddSuggestionLimit);
        }
        finally
        {
            AppSettingsProvider.NormalizeAndSetCurrent(previousCurrent);
        }
    }

    [Fact]
    public void InvokeCommit_NotifiesRefreshSubscribersAfterCommitSubscribers()
    {
        var appEvent = AppEventFactory.Create();
        var calls = new List<string>();
        appEvent.Subscribe(AppEvents.Commit, _ => calls.Add("commit"));
        appEvent.Subscribe(AppEvents.Refresh, _ => calls.Add("refresh"));

        appEvent.Invoke(AppEvents.Commit, new AppSettings());

        Assert.Equal(["commit", "refresh"], calls);
    }

    [Fact]
    public void UnsubscribeCommit_StopsNotifications()
    {
        var appEvent = AppEventFactory.Create();
        var callCount = 0;
        void Handler(AppSettings _) => callCount++;
        appEvent.Subscribe(AppEvents.Commit, Handler);
        appEvent.Unsubscribe(AppEvents.Commit, Handler);

        appEvent.Invoke(AppEvents.Commit, new AppSettings());

        Assert.Equal(0, callCount);
    }

    [Fact]
    public void InvokeRefresh_NotifiesSubscribers()
    {
        var appEvent = AppEventFactory.Create();
        AppRefreshPayload? refreshed = null;
        appEvent.Subscribe(AppEvents.Refresh, payload => refreshed = payload);
        var previousSettings = new AppSettings { LaunchAtWindowsStartup = false };
        var currentSettings = new AppSettings { LaunchAtWindowsStartup = true };
        var payload = new AppRefreshPayload(previousSettings, currentSettings);

        appEvent.Invoke(AppEvents.Refresh, payload);

        Assert.True(refreshed.HasValue);
        Assert.Same(previousSettings, refreshed.Value.PreviousSettings);
        Assert.Same(currentSettings, refreshed.Value.CurrentSettings);
    }

    [Fact]
    public void UnsubscribeRefresh_StopsNotifications()
    {
        var appEvent = AppEventFactory.Create();
        var callCount = 0;
        void Handler(AppRefreshPayload _) => callCount++;
        appEvent.Subscribe(AppEvents.Refresh, Handler);
        appEvent.Unsubscribe(AppEvents.Refresh, Handler);

        appEvent.Invoke(AppEvents.Refresh, new AppRefreshPayload(new AppSettings(), new AppSettings()));

        Assert.Equal(0, callCount);
    }

    [Fact]
    public void InvokeUpdateCheckRequested_NotifiesSubscribers()
    {
        var appEvent = AppEventFactory.Create();
        var callCount = 0;
        void Handler() => callCount++;
        appEvent.Subscribe(AppEvents.UpdateCheckRequested, Handler);

        appEvent.Invoke(AppEvents.UpdateCheckRequested);

        Assert.Equal(1, callCount);
    }

    [Fact]
    public void UnsubscribeUpdateCheckRequested_StopsNotifications()
    {
        var appEvent = AppEventFactory.Create();
        var callCount = 0;
        void Handler() => callCount++;
        appEvent.Subscribe(AppEvents.UpdateCheckRequested, Handler);
        appEvent.Unsubscribe(AppEvents.UpdateCheckRequested, Handler);

        appEvent.Invoke(AppEvents.UpdateCheckRequested);

        Assert.Equal(0, callCount);
    }

    [Fact]
    public void InvokeUpdateAvailabilityChanged_NotifiesSubscribers()
    {
        var appEvent = AppEventFactory.Create();
        AppUpdateInfo? notified = null;
        appEvent.Subscribe(AppEvents.UpdateAvailabilityChanged, update => notified = update);
        var updateInfo = new AppUpdateInfo(SemanticVersion.Parse("2.0.0"), SemanticVersion.Parse("1.0.0"), new Uri("https://example.com/download"), new Uri("https://example.com/release"));

        appEvent.Invoke(AppEvents.UpdateAvailabilityChanged, updateInfo);

        Assert.Same(updateInfo, notified);
    }

    [Fact]
    public void UnsubscribeUpdateAvailabilityChanged_StopsNotifications()
    {
        var appEvent = AppEventFactory.Create();
        var callCount = 0;
        void Handler(AppUpdateInfo? _) => callCount++;
        appEvent.Subscribe(AppEvents.UpdateAvailabilityChanged, Handler);
        appEvent.Unsubscribe(AppEvents.UpdateAvailabilityChanged, Handler);

        appEvent.Invoke(AppEvents.UpdateAvailabilityChanged, null);

        Assert.Equal(0, callCount);
    }

    [Fact]
    public void InvokeApplyUpdateRequested_NotifiesSubscribers()
    {
        var appEvent = AppEventFactory.Create();
        AppUpdateInfo? notified = null;
        appEvent.Subscribe(AppEvents.ApplyUpdateRequested, update => notified = update);
        var updateInfo = new AppUpdateInfo(SemanticVersion.Parse("2.0.0"), SemanticVersion.Parse("1.0.0"), new Uri("https://example.com/download"), new Uri("https://example.com/release"));

        appEvent.Invoke(AppEvents.ApplyUpdateRequested, updateInfo);

        Assert.Same(updateInfo, notified);
    }

    [Fact]
    public void UnsubscribeApplyUpdateRequested_StopsNotifications()
    {
        var appEvent = AppEventFactory.Create();
        var callCount = 0;
        void Handler(AppUpdateInfo _) => callCount++;
        appEvent.Subscribe(AppEvents.ApplyUpdateRequested, Handler);
        appEvent.Unsubscribe(AppEvents.ApplyUpdateRequested, Handler);
        var updateInfo = new AppUpdateInfo(SemanticVersion.Parse("2.0.0"), SemanticVersion.Parse("1.0.0"), new Uri("https://example.com/download"), new Uri("https://example.com/release"));

        appEvent.Invoke(AppEvents.ApplyUpdateRequested, updateInfo);

        Assert.Equal(0, callCount);
    }

    [Fact]
    public void Subscribe_OnSeparateInstances_DoesNotShareHandlers()
    {
        var firstAppEvent = AppEventFactory.Create();
        var secondAppEvent = AppEventFactory.Create();
        var firstCallCount = 0;
        var secondCallCount = 0;

        firstAppEvent.Subscribe(AppEvents.UpdateCheckRequested, () => firstCallCount++);
        secondAppEvent.Subscribe(AppEvents.UpdateCheckRequested, () => secondCallCount++);

        firstAppEvent.Invoke(AppEvents.UpdateCheckRequested);

        Assert.Equal(1, firstCallCount);
        Assert.Equal(0, secondCallCount);
    }

    [Fact]
    public void RegisterGenericWithoutPipeline_CreatesPayloadChannel()
    {
        var appEvent = AppEventFactory.Create();
        var eventKey = AppEvent.Register<string>("DirectPayloadRegister");
        string? received = null;

        appEvent.Subscribe(eventKey, payload => received = payload);
        appEvent.Invoke(eventKey, "hello");

        Assert.Equal("hello", received);
    }

    [Fact]
    public void RegisterSignalWithPipeline_InvokesPipelineAndHandlers()
    {
        var appEvent = AppEventFactory.Create();
        var execution = new List<string>();
        var eventKey = AppEvent.Register(
            "DirectSignalRegister",
            (current, next) =>
            {
                execution.Add("pipeline");
                Assert.Same(appEvent, current);
                next();
            });

        appEvent.Subscribe(eventKey, () => execution.Add("handler"));
        appEvent.Invoke(eventKey);

        Assert.Equal(["pipeline", "handler"], execution);
    }
}