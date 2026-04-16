using applanch.Infrastructure.Storage;
using applanch.Infrastructure.Updates;

namespace applanch.Events;

internal static class AppEvents
{
    internal static AppEventKey<AppSettings> BeforeCommit { get; } = new(AppEventType.BeforeCommit);

    internal static AppEventKey<AppSettings> Commit { get; } = new(AppEventType.Commit);

    internal static AppEventKey<AppRefreshPayload> Refresh { get; } = new(AppEventType.Refresh);

    internal static AppSignalEventKey UpdateCheckRequested { get; } = new(AppEventType.UpdateCheckRequested);

    internal static AppEventKey<AppUpdateInfo?> UpdateAvailabilityChanged { get; } = new(AppEventType.UpdateAvailabilityChanged);

    internal static AppEventKey<AppUpdateInfo> ApplyUpdateRequested { get; } = new(AppEventType.ApplyUpdateRequested);

    internal static AppEventKey<UpdateAvailabilityEvaluation> UpdateAvailabilityEvaluated { get; } = new(AppEventType.UpdateAvailabilityEvaluated);

    internal static AppSignalEventKey UpdateAutomaticApplyFailed { get; } = new(AppEventType.UpdateAutomaticApplyFailed);

    internal static AppEventKey<UpdateApplyResult> UpdateApplyFailed { get; } = new(AppEventType.UpdateApplyFailed);

    internal static AppSignalEventKey UpdateApplySucceeded { get; } = new(AppEventType.UpdateApplySucceeded);
}