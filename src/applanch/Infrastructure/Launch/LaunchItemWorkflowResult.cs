using applanch.Infrastructure.Storage;

namespace applanch.Infrastructure.Launch;

internal readonly record struct LaunchItemWorkflowResult(
    bool IsCancelled,
    LaunchExecutionResult Execution,
    PostLaunchBehavior PostLaunchBehavior)
{
    public static LaunchItemWorkflowResult Cancelled() =>
        new(true, LaunchExecutionResult.Success(), PostLaunchBehavior.KeepOpen);

    public static LaunchItemWorkflowResult Failed(LaunchExecutionResult execution) =>
        new(false, execution, PostLaunchBehavior.KeepOpen);

    public static LaunchItemWorkflowResult Succeeded(PostLaunchBehavior postLaunchBehavior) =>
        new(false, LaunchExecutionResult.Success(), postLaunchBehavior);
}
