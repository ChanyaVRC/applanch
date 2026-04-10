using System.Diagnostics;
using applanch.Events;
using applanch.Infrastructure.Storage;

namespace applanch.Infrastructure.Updates;

internal sealed class UpdateCoordinator : IDisposable
{
    private readonly AppEvent _appEvent;
    private readonly UpdateWorkflow _updateWorkflow;
    private readonly Func<UpdateInstallBehavior> _installBehaviorProvider;
    private readonly Func<AppUpdateInfo, bool> _tryBeginApply;
    private readonly Action _endApply;
    private readonly Action<AppUpdateInfo?, UpdateInstallBehavior> _onAvailabilityChanged;
    private readonly Action _onAutomaticApplyFailed;
    private readonly Action<UpdateApplyResult> _onApplyFailed;
    private readonly Action _onApplySucceeded;
    private SemanticVersion? _lastAutoApplyAttemptedVersion;
    private bool _isAutoApplyingUpdate;

    internal UpdateCoordinator(UpdateCoordinatorDependencies dependencies)
    {
        ArgumentNullException.ThrowIfNull(dependencies);

        Debug.Assert(dependencies.AppEvent is not null);
        Debug.Assert(dependencies.UpdateWorkflow is not null);
        Debug.Assert(dependencies.InstallBehaviorProvider is not null);
        Debug.Assert(dependencies.TryBeginApply is not null);
        Debug.Assert(dependencies.EndApply is not null);
        Debug.Assert(dependencies.OnAvailabilityChanged is not null);
        Debug.Assert(dependencies.OnAutomaticApplyFailed is not null);
        Debug.Assert(dependencies.OnApplyFailed is not null);
        Debug.Assert(dependencies.OnApplySucceeded is not null);

        _appEvent = dependencies.AppEvent;
        _updateWorkflow = dependencies.UpdateWorkflow;
        _installBehaviorProvider = dependencies.InstallBehaviorProvider;
        _tryBeginApply = dependencies.TryBeginApply;
        _endApply = dependencies.EndApply;
        _onAvailabilityChanged = dependencies.OnAvailabilityChanged;
        _onAutomaticApplyFailed = dependencies.OnAutomaticApplyFailed;
        _onApplyFailed = dependencies.OnApplyFailed;
        _onApplySucceeded = dependencies.OnApplySucceeded;

        _appEvent.Register(AppEvents.UpdateCheckRequested, OnUpdateCheckRequested);
        _appEvent.Register(AppEvents.UpdateAvailabilityChanged, OnUpdateAvailabilityChanged);
        _appEvent.Register(AppEvents.ApplyUpdateRequested, OnApplyUpdateRequested);
    }

    public void Dispose()
    {
        _appEvent.Unregister(AppEvents.UpdateCheckRequested, OnUpdateCheckRequested);
        _appEvent.Unregister(AppEvents.UpdateAvailabilityChanged, OnUpdateAvailabilityChanged);
        _appEvent.Unregister(AppEvents.ApplyUpdateRequested, OnApplyUpdateRequested);
    }

    internal void Reconfigure(IAppUpdateService updateService)
    {
        _updateWorkflow.SetUpdateService(updateService);
    }

    private void OnUpdateCheckRequested()
    {
        _ = RunUpdateCheckAsync();
    }

    private async Task RunUpdateCheckAsync()
    {
        var update = await _updateWorkflow.CheckForUpdateSafeAsync().ConfigureAwait(false);
        _appEvent.Invoke(AppEvents.UpdateAvailabilityChanged, update);
    }

    private void OnUpdateAvailabilityChanged(AppUpdateInfo? update)
    {
        var behavior = _installBehaviorProvider();
        _onAvailabilityChanged(update, behavior);

        if (update is null)
        {
            _lastAutoApplyAttemptedVersion = null;
            return;
        }

        if (!ShouldQueueAutomaticApply(update, behavior))
        {
            return;
        }

        _lastAutoApplyAttemptedVersion = update.NewVersion;
        _ = ApplyUpdateAsync(update, isAutomatic: true);
    }

    private bool ShouldQueueAutomaticApply(AppUpdateInfo update, UpdateInstallBehavior behavior)
    {
        if (behavior != UpdateInstallBehavior.AutomaticallyApply)
        {
            return false;
        }

        if (_isAutoApplyingUpdate)
        {
            return false;
        }

        return _lastAutoApplyAttemptedVersion != update.NewVersion;
    }

    private void OnApplyUpdateRequested(AppUpdateInfo update)
    {
        _ = ApplyUpdateAsync(update, isAutomatic: false);
    }

    private async Task ApplyUpdateAsync(AppUpdateInfo update, bool isAutomatic)
    {
        if (!_tryBeginApply(update))
        {
            return;
        }

        if (isAutomatic)
        {
            _isAutoApplyingUpdate = true;
        }

        try
        {
            var result = await _updateWorkflow.ApplyUpdateSafeAsync(update).ConfigureAwait(false);
            if (result.IsSuccess)
            {
                _onApplySucceeded();
                return;
            }

            if (isAutomatic)
            {
                _onAutomaticApplyFailed();
            }

            _onApplyFailed(result);
        }
        finally
        {
            if (isAutomatic)
            {
                _isAutoApplyingUpdate = false;
            }

            _endApply();
        }
    }
}
