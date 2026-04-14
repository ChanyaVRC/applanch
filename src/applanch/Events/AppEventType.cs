namespace applanch.Events;

internal enum AppEventType
{
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