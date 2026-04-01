using applanch.Infrastructure.Storage;
using applanch.Infrastructure.Updates;

namespace applanch;

internal static class AppEvents
{
    internal static AppEventKey<AppSettings> Commit { get; } = new(AppEventType.Commit);

    internal static AppEventKey<AppSettings> Refresh { get; } = new(AppEventType.Refresh);

    internal static AppSignalEventKey UpdateCheckRequested { get; } = new(AppEventType.UpdateCheckRequested);

    internal static AppEventKey<AppUpdateInfo?> UpdateAvailabilityChanged { get; } = new(AppEventType.UpdateAvailabilityChanged);
}