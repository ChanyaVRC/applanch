using applanch.Infrastructure.Updates;

namespace applanch.Tests.ViewModels.TestDoubles;

internal sealed class FakeUpdateService : IAppUpdateService
{
    public Task<IReadOnlyList<AppUpdateInfo>> GetAvailableUpdatesAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult<IReadOnlyList<AppUpdateInfo>>([]);
    }

    public Task<AppUpdateInfo?> CheckForUpdateAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult<AppUpdateInfo?>(null);
    }

    public Task ApplyUpdateAsync(AppUpdateInfo update, CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }
}
