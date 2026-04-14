using applanch.Infrastructure.Storage;

namespace applanch.Infrastructure.Updates;

internal readonly record struct UpdateAvailabilityEvaluation(
    AppUpdateInfo? Update,
    UpdateInstallBehavior InstallBehavior);
