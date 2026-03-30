namespace applanch;

internal interface IAppUpdateService
{
    Task<AppUpdateInfo?> CheckForUpdateAsync(CancellationToken cancellationToken = default);
    Task ApplyUpdateAsync(AppUpdateInfo update, CancellationToken cancellationToken = default);
}
