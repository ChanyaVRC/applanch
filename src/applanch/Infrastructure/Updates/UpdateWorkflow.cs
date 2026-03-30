using applanch.Infrastructure.Utilities;

namespace applanch.Infrastructure.Updates;

internal sealed class UpdateWorkflow(IAppUpdateService updateService)
{
    private IAppUpdateService _updateService = updateService;

    internal void SetUpdateService(IAppUpdateService updateService)
    {
        _updateService = updateService;
    }

    internal async Task<AppUpdateInfo?> CheckForUpdateSafeAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            return await _updateService.CheckForUpdateAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            AppLogger.Instance.Error(ex, "Update check failed");
            return null;
        }
    }

    internal async Task<UpdateApplyResult> ApplyUpdateSafeAsync(AppUpdateInfo update, CancellationToken cancellationToken = default)
    {
        try
        {
            await _updateService.ApplyUpdateAsync(update, cancellationToken).ConfigureAwait(false);
            return UpdateApplyResult.Success();
        }
        catch (Exception ex)
        {
            AppLogger.Instance.Error(ex, "Update apply failed");
            return UpdateApplyResult.Failed(ex.Message);
        }
    }
}
