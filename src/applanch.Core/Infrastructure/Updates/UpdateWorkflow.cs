using applanch.Utilities;
using applanch.Updates;

namespace applanch.Infrastructure.Updates;

public sealed class UpdateWorkflow
{
    private IAppUpdateService _updateService;

    public UpdateWorkflow(IAppUpdateService updateService)
    {
        ArgumentNullException.ThrowIfNull(updateService);
        _updateService = updateService;
    }

    public void SetUpdateService(IAppUpdateService updateService)
    {
        ArgumentNullException.ThrowIfNull(updateService);
        _updateService = updateService;
    }

    public async Task<IReadOnlyList<AppUpdateInfo>> GetAvailableUpdatesSafeAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            return await _updateService.GetAvailableUpdatesAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            AppLogger.Instance.Error(ex, "Loading available updates failed");
            return [];
        }
    }

    public async Task<AppUpdateInfo?> CheckForUpdateSafeAsync(CancellationToken cancellationToken = default)
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

    public async Task<UpdateApplyResult> ApplyUpdateSafeAsync(AppUpdateInfo update, CancellationToken cancellationToken = default)
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
            return UpdateApplyResult.Failed(MapFailureReason(ex), ex.Message);
        }
    }

    private static UpdateApplyFailureReason MapFailureReason(Exception ex)
    {
        return ex switch
        {
            HttpRequestException => UpdateApplyFailureReason.Network,
            UnauthorizedAccessException => UpdateApplyFailureReason.Permission,
            InvalidDataException => UpdateApplyFailureReason.InvalidPackage,
            IOException => UpdateApplyFailureReason.Io,
            _ => UpdateApplyFailureReason.Unknown,
        };
    }
}
