using applanch.Infrastructure.Storage;

namespace applanch.Infrastructure.Items;

internal sealed class DeleteItemWorkflow
{
    internal DeleteItemWorkflowResult TryDelete(
        LaunchItemViewModel item,
        AppSettings settings,
        Func<bool> confirmDelete,
        IList<LaunchItemViewModel> launchItems,
        Action<LaunchItemViewModel> remove)
    {
        if (settings.ConfirmBeforeDelete && !confirmDelete())
        {
            return DeleteItemWorkflowResult.Cancelled();
        }

        var index = launchItems.IndexOf(item);
        remove(item);
        return DeleteItemWorkflowResult.Succeeded(index);
    }
}
