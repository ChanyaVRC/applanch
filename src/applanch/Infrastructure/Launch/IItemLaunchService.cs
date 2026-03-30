namespace applanch;

internal interface IItemLaunchService
{
    LaunchExecutionResult TryLaunch(LaunchItemViewModel item);
}
