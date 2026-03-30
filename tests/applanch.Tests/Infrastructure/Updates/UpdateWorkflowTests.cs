using applanch.Infrastructure.Updates;
using Xunit;

namespace applanch.Tests.Infrastructure.Updates;

public class UpdateWorkflowTests
{
    [Fact]
    public async Task CheckForUpdateSafeAsync_ReturnsUpdate_WhenServiceSucceeds()
    {
        var expected = new AppUpdateInfo("2.0.0", "1.0.0", "https://example.com/a.zip", "https://example.com/r");
        var workflow = new UpdateWorkflow(new FakeAppUpdateService
        {
            CheckResult = expected,
        });

        var result = await workflow.CheckForUpdateSafeAsync();

        Assert.Equal(expected, result);
    }

    [Fact]
    public async Task CheckForUpdateSafeAsync_ReturnsNull_WhenServiceThrows()
    {
        var workflow = new UpdateWorkflow(new FakeAppUpdateService
        {
            ThrowOnCheck = true,
        });

        var result = await workflow.CheckForUpdateSafeAsync();

        Assert.Null(result);
    }

    [Fact]
    public async Task ApplyUpdateSafeAsync_ReturnsSuccess_WhenServiceSucceeds()
    {
        var update = new AppUpdateInfo("2.0.0", "1.0.0", "https://example.com/a.zip", "https://example.com/r");
        var workflow = new UpdateWorkflow(new FakeAppUpdateService());

        var result = await workflow.ApplyUpdateSafeAsync(update);

        Assert.True(result.IsSuccess);
        Assert.Equal(string.Empty, result.ErrorMessage);
    }

    [Fact]
    public async Task ApplyUpdateSafeAsync_ReturnsFailure_WhenServiceThrows()
    {
        var update = new AppUpdateInfo("2.0.0", "1.0.0", "https://example.com/a.zip", "https://example.com/r");
        var workflow = new UpdateWorkflow(new FakeAppUpdateService
        {
            ThrowOnApply = true,
        });

        var result = await workflow.ApplyUpdateSafeAsync(update);

        Assert.False(result.IsSuccess);
        Assert.Contains("apply failed", result.ErrorMessage, System.StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task SetUpdateService_ReplacesBehavior()
    {
        var update = new AppUpdateInfo("2.0.0", "1.0.0", "https://example.com/a.zip", "https://example.com/r");
        var workflow = new UpdateWorkflow(new FakeAppUpdateService
        {
            ThrowOnApply = true,
        });

        workflow.SetUpdateService(new FakeAppUpdateService());
        var result = await workflow.ApplyUpdateSafeAsync(update);

        Assert.True(result.IsSuccess);
    }

    private sealed class FakeAppUpdateService : IAppUpdateService
    {
        internal AppUpdateInfo? CheckResult { get; init; }
        internal bool ThrowOnCheck { get; init; }
        internal bool ThrowOnApply { get; init; }

        public Task<AppUpdateInfo?> CheckForUpdateAsync(System.Threading.CancellationToken cancellationToken = default)
        {
            if (ThrowOnCheck)
            {
                throw new InvalidOperationException("check failed");
            }

            return Task.FromResult(CheckResult);
        }

        public Task ApplyUpdateAsync(AppUpdateInfo update, System.Threading.CancellationToken cancellationToken = default)
        {
            if (ThrowOnApply)
            {
                throw new InvalidOperationException("apply failed");
            }

            return Task.CompletedTask;
        }
    }
}
