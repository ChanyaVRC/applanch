using applanch.Events;
using applanch.Infrastructure.Storage;
using applanch.Infrastructure.Updates;
using applanch.Tests.TestSupport;
using Xunit;

namespace applanch.Tests.Infrastructure.Updates;

public class UpdateCoordinatorTests
{
    [Fact]
    public async Task UpdateCheckRequested_PublishesAvailabilityToUiCallback()
    {
        var appEvent = AppEventFactory.Create();
        var update = CreateUpdate("1.2.0");
        var service = new FakeAppUpdateService { CheckResult = update };
        var callback = new TaskCompletionSource<AppUpdateInfo?>(TaskCreationOptions.RunContinuationsAsynchronously);
        using var coordinator = CreateCoordinator(
            appEvent,
            service,
            onAvailabilityChanged: (availableUpdate, _) => callback.TrySetResult(availableUpdate));

        appEvent.Invoke(AppEvents.UpdateCheckRequested);

        var completed = await Task.WhenAny(callback.Task, Task.Delay(TimeSpan.FromSeconds(2)));
        Assert.Same(callback.Task, completed);
        var publishedUpdate = await callback.Task;
        Assert.Same(update, publishedUpdate);
    }

    [Fact]
    public async Task ApplyUpdateRequested_DoesNotApply_WhenUiRejectsStart()
    {
        var appEvent = AppEventFactory.Create();
        var service = new FakeAppUpdateService();
        using var coordinator = CreateCoordinator(appEvent, service, tryBeginApply: static _ => false);

        appEvent.Invoke(AppEvents.ApplyUpdateRequested, CreateUpdate("1.2.0"));
        await Task.Delay(100);

        Assert.Equal(0, service.ApplyCallCount);
    }

    [Fact]
    public async Task UpdateAvailabilityChanged_AutomaticMode_AppliesOnlyOncePerVersion()
    {
        var appEvent = AppEventFactory.Create();
        var service = new FakeAppUpdateService();
        var update = CreateUpdate("1.2.0");
        using var coordinator = CreateCoordinator(appEvent, service, static () => UpdateInstallBehavior.AutomaticallyApply);

        appEvent.Invoke(AppEvents.UpdateAvailabilityChanged, update);
        appEvent.Invoke(AppEvents.UpdateAvailabilityChanged, update);
        await Task.Delay(100);

        Assert.Equal(1, service.ApplyCallCount);
    }

    private static UpdateCoordinator CreateCoordinator(
        AppEvent appEvent,
        FakeAppUpdateService service,
        Func<UpdateInstallBehavior>? installBehaviorProvider = null,
        Func<AppUpdateInfo, bool>? tryBeginApply = null,
        Action<AppUpdateInfo?, UpdateInstallBehavior>? onAvailabilityChanged = null)
    {
        return new UpdateCoordinator(new UpdateCoordinatorDependencies
        {
            AppEvent = appEvent,
            UpdateWorkflow = new UpdateWorkflow(service),
            InstallBehaviorProvider = installBehaviorProvider ?? (static () => UpdateInstallBehavior.Manual),
            TryBeginApply = tryBeginApply ?? (static _ => true),
            EndApply = static () => { },
            OnAvailabilityChanged = onAvailabilityChanged ?? (static (_, _) => { }),
            OnAutomaticApplyFailed = static () => { },
            OnApplyFailed = static _ => { },
            OnApplySucceeded = static () => { },
        });
    }

    private static AppUpdateInfo CreateUpdate(string version)
    {
        return new AppUpdateInfo(
            SemanticVersion.Parse(version),
            SemanticVersion.Parse("1.0.0"),
            new Uri("https://example.com/a.zip"),
            new Uri("https://example.com/r"));
    }

    private sealed class FakeAppUpdateService : IAppUpdateService
    {
        internal AppUpdateInfo? CheckResult { get; init; }
        internal int ApplyCallCount { get; private set; }

        public Task<IReadOnlyList<AppUpdateInfo>> GetAvailableUpdatesAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult<IReadOnlyList<AppUpdateInfo>>([]);
        }

        public Task<AppUpdateInfo?> CheckForUpdateAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult(CheckResult);
        }

        public Task ApplyUpdateAsync(AppUpdateInfo update, CancellationToken cancellationToken = default)
        {
            ApplyCallCount++;
            return Task.CompletedTask;
        }
    }
}
