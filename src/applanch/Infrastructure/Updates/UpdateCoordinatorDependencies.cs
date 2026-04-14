using applanch.Events;
using applanch.Infrastructure.Storage;

namespace applanch.Infrastructure.Updates;

internal sealed class UpdateCoordinatorDependencies
{
    internal required AppEvent AppEvent { get; init; }

    internal required UpdateWorkflow UpdateWorkflow { get; init; }

    internal required UpdateInstallBehavior InitialInstallBehavior { get; init; }

    internal required Func<AppUpdateInfo, bool> TryBeginApply { get; init; }

    internal required Action EndApply { get; init; }
}
