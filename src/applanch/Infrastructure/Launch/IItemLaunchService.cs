namespace applanch.Infrastructure.Launch;

internal interface IItemLaunchService
{
    LaunchExecutionResult TryLaunch(LaunchItemViewModel item, bool runAsAdministrator = false);
}

