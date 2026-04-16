using applanch.Events;
using applanch.Infrastructure.Storage;
using applanch.Infrastructure.Updates;
using applanch.Tests.TestSupport;
using Xunit;

namespace applanch.Tests.Application;

public class AppEventTests
{
    [Fact]
    public void InvokeCommit_NotifiesRegisteredHandlers()
    {
        var appEvent = AppEventFactory.Create();
        AppSettings? committed = null;
        appEvent.Register(AppEvents.Commit, settings => committed = settings);
        var settings = new AppSettings { DebugUpdate = true };

        appEvent.Invoke(AppEvents.Commit, settings);

        Assert.Same(settings, committed);
    }

    [Fact]
    public void UnregisterCommit_StopsNotifications()
    {
        var appEvent = AppEventFactory.Create();
        var callCount = 0;
        void Handler(AppSettings _) => callCount++;
        appEvent.Register(AppEvents.Commit, Handler);
        appEvent.Unregister(AppEvents.Commit, Handler);

        appEvent.Invoke(AppEvents.Commit, new AppSettings());

        Assert.Equal(0, callCount);
    }

    [Fact]
    public void InvokeRefresh_NotifiesRegisteredHandlers()
    {
        var appEvent = AppEventFactory.Create();
        AppRefreshPayload? refreshed = null;
        appEvent.Register(AppEvents.Refresh, payload => refreshed = payload);
        var previousSettings = new AppSettings { LaunchAtWindowsStartup = false };
        var currentSettings = new AppSettings { LaunchAtWindowsStartup = true };
        var payload = new AppRefreshPayload(previousSettings, currentSettings);

        appEvent.Invoke(AppEvents.Refresh, payload);

        Assert.True(refreshed.HasValue);
        Assert.Same(previousSettings, refreshed.Value.PreviousSettings);
        Assert.Same(currentSettings, refreshed.Value.CurrentSettings);
    }

    [Fact]
    public void UnregisterRefresh_StopsNotifications()
    {
        var appEvent = AppEventFactory.Create();
        var callCount = 0;
        void Handler(AppRefreshPayload _) => callCount++;
        appEvent.Register(AppEvents.Refresh, Handler);
        appEvent.Unregister(AppEvents.Refresh, Handler);

        appEvent.Invoke(AppEvents.Refresh, new AppRefreshPayload(new AppSettings(), new AppSettings()));

        Assert.Equal(0, callCount);
    }

    [Fact]
    public void InvokeUpdateCheckRequested_NotifiesRegisteredHandlers()
    {
        var appEvent = AppEventFactory.Create();
        var callCount = 0;
        void Handler() => callCount++;
        appEvent.Register(AppEvents.UpdateCheckRequested, Handler);

        appEvent.Invoke(AppEvents.UpdateCheckRequested);

        Assert.Equal(1, callCount);
    }

    [Fact]
    public void UnregisterUpdateCheckRequested_StopsNotifications()
    {
        var appEvent = AppEventFactory.Create();
        var callCount = 0;
        void Handler() => callCount++;
        appEvent.Register(AppEvents.UpdateCheckRequested, Handler);
        appEvent.Unregister(AppEvents.UpdateCheckRequested, Handler);

        appEvent.Invoke(AppEvents.UpdateCheckRequested);

        Assert.Equal(0, callCount);
    }

    [Fact]
    public void InvokeUpdateAvailabilityChanged_NotifiesRegisteredHandlers()
    {
        var appEvent = AppEventFactory.Create();
        AppUpdateInfo? notified = null;
        appEvent.Register(AppEvents.UpdateAvailabilityChanged, update => notified = update);
        var updateInfo = new AppUpdateInfo(SemanticVersion.Parse("2.0.0"), SemanticVersion.Parse("1.0.0"), new Uri("https://example.com/download"), new Uri("https://example.com/release"));

        appEvent.Invoke(AppEvents.UpdateAvailabilityChanged, updateInfo);

        Assert.Same(updateInfo, notified);
    }

    [Fact]
    public void UnregisterUpdateAvailabilityChanged_StopsNotifications()
    {
        var appEvent = AppEventFactory.Create();
        var callCount = 0;
        void Handler(AppUpdateInfo? _) => callCount++;
        appEvent.Register(AppEvents.UpdateAvailabilityChanged, Handler);
        appEvent.Unregister(AppEvents.UpdateAvailabilityChanged, Handler);

        appEvent.Invoke(AppEvents.UpdateAvailabilityChanged, null);

        Assert.Equal(0, callCount);
    }

    [Fact]
    public void InvokeApplyUpdateRequested_NotifiesRegisteredHandlers()
    {
        var appEvent = AppEventFactory.Create();
        AppUpdateInfo? notified = null;
        appEvent.Register(AppEvents.ApplyUpdateRequested, update => notified = update);
        var updateInfo = new AppUpdateInfo(SemanticVersion.Parse("2.0.0"), SemanticVersion.Parse("1.0.0"), new Uri("https://example.com/download"), new Uri("https://example.com/release"));

        appEvent.Invoke(AppEvents.ApplyUpdateRequested, updateInfo);

        Assert.Same(updateInfo, notified);
    }

    [Fact]
    public void UnregisterApplyUpdateRequested_StopsNotifications()
    {
        var appEvent = AppEventFactory.Create();
        var callCount = 0;
        void Handler(AppUpdateInfo _) => callCount++;
        appEvent.Register(AppEvents.ApplyUpdateRequested, Handler);
        appEvent.Unregister(AppEvents.ApplyUpdateRequested, Handler);
        var updateInfo = new AppUpdateInfo(SemanticVersion.Parse("2.0.0"), SemanticVersion.Parse("1.0.0"), new Uri("https://example.com/download"), new Uri("https://example.com/release"));

        appEvent.Invoke(AppEvents.ApplyUpdateRequested, updateInfo);

        Assert.Equal(0, callCount);
    }
}
