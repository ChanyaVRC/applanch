using applanch.Settings;

namespace applanch.Updates;

public readonly record struct UpdateAvailabilityEvaluation(
    AppUpdateInfo? Update,
    UpdateInstallBehavior InstallBehavior);
