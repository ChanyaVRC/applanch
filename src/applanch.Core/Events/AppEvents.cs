using applanch.Settings;
using applanch.Updates;

namespace applanch.Events;

public static class AppEvents
{
    public static AppEventKey<AppSettings> BeforeCommit { get; } = new(AppEventType.BeforeCommit);

    public static AppEventKey<AppSettings> Commit { get; } = new(AppEventType.Commit);

    public static AppEventKey<AppRefreshPayload> Refresh { get; } = new(AppEventType.Refresh);

    public static AppSignalEventKey UpdateCheckRequested { get; } = new(AppEventType.UpdateCheckRequested);

    public static AppEventKey<AppUpdateInfo?> UpdateAvailabilityChanged { get; } = new(AppEventType.UpdateAvailabilityChanged);

    public static AppEventKey<AppUpdateInfo> ApplyUpdateRequested { get; } = new(AppEventType.ApplyUpdateRequested);

    public static AppEventKey<UpdateAvailabilityEvaluation> UpdateAvailabilityEvaluated { get; } = new(AppEventType.UpdateAvailabilityEvaluated);

    public static AppSignalEventKey UpdateAutomaticApplyFailed { get; } = new(AppEventType.UpdateAutomaticApplyFailed);

    public static AppEventKey<UpdateApplyResult> UpdateApplyFailed { get; } = new(AppEventType.UpdateApplyFailed);

    public static AppSignalEventKey UpdateApplySucceeded { get; } = new(AppEventType.UpdateApplySucceeded);
}
