using applanch.Infrastructure.Storage;
using applanch.ViewModels;

namespace applanch.Infrastructure.Launch;

internal sealed class LaunchItemWorkflow(IItemLaunchService itemLaunchService)
{
    internal LaunchItemWorkflowResult TryLaunch(
        LaunchItemViewModel item,
        AppSettings settings,
        Func<bool> confirmLaunch)
    {
        if (settings.ConfirmBeforeLaunch && !confirmLaunch())
        {
            return LaunchItemWorkflowResult.Cancelled();
        }

        var execution = itemLaunchService.TryLaunch(item, settings.RunAsAdministrator);
        if (!execution.IsSuccess)
        {
            return LaunchItemWorkflowResult.Failed(execution);
        }

        return LaunchItemWorkflowResult.Succeeded(settings.ResolvePostLaunchBehavior());
    }
}
