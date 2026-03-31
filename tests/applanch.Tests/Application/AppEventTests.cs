using applanch.Infrastructure.Storage;
using applanch.Infrastructure.Updates;
using Xunit;

namespace applanch.Tests;

public class AppEventTests
{
    [Fact]
    public void InvokeCommit_NotifiesRegisteredHandlers()
    {
        var appEvent = new AppEvent();
        AppSettings? committed = null;
        appEvent.Register(AppEvents.Commit, settings => committed = settings);
        var settings = new AppSettings { DebugUpdate = true };

        appEvent.Invoke(AppEvents.Commit, settings);

        Assert.Same(settings, committed);
    }

    [Fact]
    public void UnregisterCommit_StopsNotifications()
    {
        var appEvent = new AppEvent();
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
        var appEvent = new AppEvent();
        AppSettings? refreshed = null;
        appEvent.Register(AppEvents.Refresh, settings => refreshed = settings);
        var settings = new AppSettings { LaunchAtWindowsStartup = true };

        appEvent.Invoke(AppEvents.Refresh, settings);

        Assert.Same(settings, refreshed);
    }

    [Fact]
    public void UnregisterRefresh_StopsNotifications()
    {
        var appEvent = new AppEvent();
        var callCount = 0;
        void Handler(AppSettings _) => callCount++;
        appEvent.Register(AppEvents.Refresh, Handler);
        appEvent.Unregister(AppEvents.Refresh, Handler);

        appEvent.Invoke(AppEvents.Refresh, new AppSettings());

        Assert.Equal(0, callCount);
    }

    [Fact]
    public void InvokeUpdateCheckRequested_NotifiesRegisteredHandlers()
    {
        var appEvent = new AppEvent();
        var callCount = 0;
        void Handler() => callCount++;
        appEvent.Register(AppEvents.UpdateCheckRequested, Handler);

        appEvent.Invoke(AppEvents.UpdateCheckRequested);

        Assert.Equal(1, callCount);
    }

    [Fact]
    public void UnregisterUpdateCheckRequested_StopsNotifications()
    {
        var appEvent = new AppEvent();
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
        var appEvent = new AppEvent();
        AppUpdateInfo? notified = null;
        appEvent.Register(AppEvents.UpdateAvailabilityChanged, update => notified = update);
        var updateInfo = new AppUpdateInfo("2.0.0", "1.0.0", "https://example.com/download", "https://example.com/release");

        appEvent.Invoke(AppEvents.UpdateAvailabilityChanged, updateInfo);

        Assert.Same(updateInfo, notified);
    }

    [Fact]
    public void UnregisterUpdateAvailabilityChanged_StopsNotifications()
    {
        var appEvent = new AppEvent();
        var callCount = 0;
        void Handler(AppUpdateInfo? _) => callCount++;
        appEvent.Register(AppEvents.UpdateAvailabilityChanged, Handler);
        appEvent.Unregister(AppEvents.UpdateAvailabilityChanged, Handler);

        appEvent.Invoke(AppEvents.UpdateAvailabilityChanged, null);

        Assert.Equal(0, callCount);
    }
}
