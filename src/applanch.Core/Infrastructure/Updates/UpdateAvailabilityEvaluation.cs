using applanch.Infrastructure.Storage;

namespace applanch.Infrastructure.Updates;

public readonly record struct UpdateAvailabilityEvaluation(
    AppUpdateInfo? Update,
    UpdateInstallBehavior InstallBehavior);
