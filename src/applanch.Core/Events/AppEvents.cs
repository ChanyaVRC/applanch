using applanch.Infrastructure.Storage;
using applanch.Settings;
using applanch.Updates;

namespace applanch.Events;

public static class AppEvents
{
    public static AppEventKey<AppRefreshPayload> Refresh { get; } = AppEvent.Register<AppRefreshPayload>(nameof(Refresh));

    public static AppEventKey<AppSettings> Commit { get; } = AppEvent.Register<AppSettings>(
        nameof(Commit),
        static (appEvent, settings, next) =>
        {
            var previousSettings = AppSettingsProvider.Current;
            var normalizedSettings = AppSettingsProvider.NormalizeAndSetCurrent(settings);

            next(normalizedSettings);
            appEvent.Invoke(Refresh, new AppRefreshPayload(previousSettings, normalizedSettings));
        });

    public static AppEventKey UpdateCheckRequested { get; } = AppEvent.Register(nameof(UpdateCheckRequested));

    public static AppEventKey<AppUpdateInfo?> UpdateAvailabilityChanged { get; } = AppEvent.Register<AppUpdateInfo?>(nameof(UpdateAvailabilityChanged));

    public static AppEventKey<AppUpdateInfo> ApplyUpdateRequested { get; } = AppEvent.Register<AppUpdateInfo>(nameof(ApplyUpdateRequested));

    public static AppEventKey<UpdateAvailabilityEvaluation> UpdateAvailabilityEvaluated { get; } = AppEvent.Register<UpdateAvailabilityEvaluation>(nameof(UpdateAvailabilityEvaluated));

    public static AppEventKey UpdateAutomaticApplyFailed { get; } = AppEvent.Register(nameof(UpdateAutomaticApplyFailed));

    public static AppEventKey<UpdateApplyResult> UpdateApplyFailed { get; } = AppEvent.Register<UpdateApplyResult>(nameof(UpdateApplyFailed));

    public static AppEventKey UpdateApplySucceeded { get; } = AppEvent.Register(nameof(UpdateApplySucceeded));
}
