using applanch.Infrastructure.Storage;

namespace applanch.Infrastructure.Updates;

internal sealed class UpdateAvailabilityCoordinator
{
    private AppUpdateInfo? _pendingUpdate;
    private string? _lastAutoApplyAttemptedVersion;
    private bool _isAutoApplyingUpdate;

    internal AppUpdateInfo? PendingUpdate => _pendingUpdate;

    internal bool ShouldAutoApply(AppUpdateInfo? update, UpdateInstallBehavior installBehavior)
    {
        _pendingUpdate = update;

        if (update is null)
        {
            _lastAutoApplyAttemptedVersion = null;
            return false;
        }

        if (installBehavior != UpdateInstallBehavior.AutomaticallyApply)
        {
            return false;
        }

        if (_isAutoApplyingUpdate)
        {
            return false;
        }

        if (string.Equals(_lastAutoApplyAttemptedVersion, update.NewVersion, StringComparison.Ordinal))
        {
            return false;
        }

        _lastAutoApplyAttemptedVersion = update.NewVersion;
        return true;
    }

    internal void BeginAutomaticApply()
    {
        _isAutoApplyingUpdate = true;
    }

    internal void EndAutomaticApply()
    {
        _isAutoApplyingUpdate = false;
    }
}