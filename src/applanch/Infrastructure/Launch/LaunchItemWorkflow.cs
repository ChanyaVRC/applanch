using applanch.Infrastructure.Storage;

namespace applanch.Infrastructure.Launch;

internal sealed class LaunchItemWorkflow(IItemLaunchService itemLaunchService)
{
    private readonly IItemLaunchService _itemLaunchService = itemLaunchService;

    internal LaunchItemWorkflowResult TryLaunch(
        LaunchItemViewModel item,
        AppSettings settings,
        Func<bool> confirmLaunch)
    {
        if (settings.ConfirmBeforeLaunch && !confirmLaunch())
        {
            return LaunchItemWorkflowResult.Cancelled();
        }

        var execution = _itemLaunchService.TryLaunch(item, settings.RunAsAdministrator);
        if (!execution.IsSuccess)
        {
            return LaunchItemWorkflowResult.Failed(execution);
        }

        return LaunchItemWorkflowResult.Succeeded(settings.ResolvePostLaunchBehavior());
    }
}
