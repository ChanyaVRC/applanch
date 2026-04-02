using applanch.Infrastructure.Utilities;

namespace applanch.Infrastructure.Launch;

internal interface IItemLaunchService
{
    LaunchExecutionResult TryLaunch(LaunchPath launchPath, string arguments, bool runAsAdministrator = false);
}

