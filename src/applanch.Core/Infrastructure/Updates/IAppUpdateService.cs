namespace applanch.Infrastructure.Updates;

public interface IAppUpdateService
{
    Task<IReadOnlyList<AppUpdateInfo>> GetAvailableUpdatesAsync(CancellationToken cancellationToken = default);

    Task<AppUpdateInfo?> CheckForUpdateAsync(CancellationToken cancellationToken = default);

    Task ApplyUpdateAsync(AppUpdateInfo update, CancellationToken cancellationToken = default);
}