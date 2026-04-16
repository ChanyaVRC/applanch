using applanch.Infrastructure.Launch;

namespace applanch.Tests.ViewModels.TestDoubles;

internal sealed class FakeLaunchService : IItemLaunchService
{
    public LaunchExecutionResult TryLaunch(LaunchPath launchPath, string arguments, bool runAsAdministrator = false)
    {
        return LaunchExecutionResult.Success();
    }
}
