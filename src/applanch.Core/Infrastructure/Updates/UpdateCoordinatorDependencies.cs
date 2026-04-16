using applanch.Events;
using applanch.Infrastructure.Storage;

namespace applanch.Infrastructure.Updates;

public sealed class UpdateCoordinatorDependencies
{
    public required AppEvent AppEvent { get; init; }

    public required UpdateWorkflow UpdateWorkflow { get; init; }

    public required UpdateInstallBehavior InitialInstallBehavior { get; init; }

    public required Func<AppUpdateInfo, bool> TryBeginApply { get; init; }

    public required Action EndApply { get; init; }
}