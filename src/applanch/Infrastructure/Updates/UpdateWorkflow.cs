using applanch.Infrastructure.Utilities;

namespace applanch.Infrastructure.Updates;

internal sealed class UpdateWorkflow
{
    private IAppUpdateService _updateService;

    internal UpdateWorkflow(IAppUpdateService updateService)
    {
        ArgumentNullException.ThrowIfNull(updateService);
        _updateService = updateService;
    }

    internal void SetUpdateService(IAppUpdateService updateService)
    {
        ArgumentNullException.ThrowIfNull(updateService);
        _updateService = updateService;
    }

    internal async Task<AppUpdateInfo?> CheckForUpdateSafeAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            return await _updateService.CheckForUpdateAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            throw;
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
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            AppLogger.Instance.Error(ex, "Update apply failed");
            return UpdateApplyResult.Failed(ex.Message);
        }
    }
}
