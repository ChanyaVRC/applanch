namespace applanch.Events;

internal enum AppEventType
{
    BeforeCommit,
    Commit,
    Refresh,
    UpdateCheckRequested,
    UpdateAvailabilityChanged,
    ApplyUpdateRequested,
    UpdateAvailabilityEvaluated,
    UpdateAutomaticApplyFailed,
    UpdateApplyFailed,
    UpdateApplySucceeded,
}