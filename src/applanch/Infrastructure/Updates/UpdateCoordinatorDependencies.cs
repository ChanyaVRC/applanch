using applanch.Events;
using applanch.Infrastructure.Storage;

namespace applanch.Infrastructure.Updates;

internal sealed class UpdateCoordinatorDependencies
{
    internal required AppEvent AppEvent { get; init; }

    internal required UpdateWorkflow UpdateWorkflow { get; init; }

    internal required Func<UpdateInstallBehavior> InstallBehaviorProvider { get; init; }

    internal required Func<AppUpdateInfo, bool> TryBeginApply { get; init; }

    internal required Action EndApply { get; init; }

    internal required Action<AppUpdateInfo?, UpdateInstallBehavior> OnAvailabilityChanged { get; init; }

    internal required Action OnAutomaticApplyFailed { get; init; }

    internal required Action<UpdateApplyResult> OnApplyFailed { get; init; }

    internal required Action OnApplySucceeded { get; init; }
}
