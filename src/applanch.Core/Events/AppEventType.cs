namespace applanch.Events;

public enum AppEventType
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
