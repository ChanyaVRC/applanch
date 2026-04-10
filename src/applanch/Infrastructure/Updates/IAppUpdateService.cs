namespace applanch.Infrastructure.Updates;

internal interface IAppUpdateService
{
    Task<IReadOnlyList<AppUpdateInfo>> GetAvailableUpdatesAsync(CancellationToken cancellationToken = default);
    Task<AppUpdateInfo?> CheckForUpdateAsync(CancellationToken cancellationToken = default);
    Task ApplyUpdateAsync(AppUpdateInfo update, CancellationToken cancellationToken = default);
}

