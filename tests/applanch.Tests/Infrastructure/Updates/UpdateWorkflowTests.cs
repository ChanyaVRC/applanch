using applanch.Infrastructure.Updates;
using Xunit;

namespace applanch.Tests.Infrastructure.Updates;

public class UpdateWorkflowTests
{
    [Fact]
    public void Constructor_WhenServiceIsNull_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => new UpdateWorkflow(null!));
    }

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
    public async Task CheckForUpdateSafeAsync_PropagatesCancellation()
    {
        var workflow = new UpdateWorkflow(new FakeAppUpdateService
        {
            ThrowCanceledOnCheck = true,
        });

        await Assert.ThrowsAsync<OperationCanceledException>(() => workflow.CheckForUpdateSafeAsync());
    }

    [Fact]
    public async Task ApplyUpdateSafeAsync_ReturnsSuccess_WhenServiceSucceeds()
    {
        var update = new AppUpdateInfo("2.0.0", "1.0.0", "https://example.com/a.zip", "https://example.com/r");
        var workflow = new UpdateWorkflow(new FakeAppUpdateService());

        var result = await workflow.ApplyUpdateSafeAsync(update);

        Assert.True(result.IsSuccess);
        Assert.Equal(UpdateApplyFailureReason.None, result.FailureReason);
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
        Assert.Equal(UpdateApplyFailureReason.Unknown, result.FailureReason);
        Assert.Contains("apply failed", result.ErrorMessage, System.StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ApplyUpdateSafeAsync_ReturnsNetworkFailureReason_WhenHttpThrows()
    {
        var update = new AppUpdateInfo("2.0.0", "1.0.0", "https://example.com/a.zip", "https://example.com/r");
        var workflow = new UpdateWorkflow(new FakeAppUpdateService
        {
            ThrowHttpOnApply = true,
        });

        var result = await workflow.ApplyUpdateSafeAsync(update);

        Assert.False(result.IsSuccess);
        Assert.Equal(UpdateApplyFailureReason.Network, result.FailureReason);
    }

    [Fact]
    public async Task ApplyUpdateSafeAsync_ReturnsIoFailureReason_WhenIoThrows()
    {
        var update = new AppUpdateInfo("2.0.0", "1.0.0", "https://example.com/a.zip", "https://example.com/r");
        var workflow = new UpdateWorkflow(new FakeAppUpdateService
        {
            ThrowIoOnApply = true,
        });

        var result = await workflow.ApplyUpdateSafeAsync(update);

        Assert.False(result.IsSuccess);
        Assert.Equal(UpdateApplyFailureReason.Io, result.FailureReason);
    }

    [Fact]
    public async Task ApplyUpdateSafeAsync_PropagatesCancellation()
    {
        var update = new AppUpdateInfo("2.0.0", "1.0.0", "https://example.com/a.zip", "https://example.com/r");
        var workflow = new UpdateWorkflow(new FakeAppUpdateService
        {
            ThrowCanceledOnApply = true,
        });

        await Assert.ThrowsAsync<OperationCanceledException>(() => workflow.ApplyUpdateSafeAsync(update));
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

    [Fact]
    public void SetUpdateService_WhenServiceIsNull_ThrowsArgumentNullException()
    {
        var workflow = new UpdateWorkflow(new FakeAppUpdateService());

        Assert.Throws<ArgumentNullException>(() => workflow.SetUpdateService(null!));
    }

    private sealed class FakeAppUpdateService : IAppUpdateService
    {
        internal AppUpdateInfo? CheckResult { get; init; }
        internal bool ThrowOnCheck { get; init; }
        internal bool ThrowOnApply { get; init; }
        internal bool ThrowCanceledOnCheck { get; init; }
        internal bool ThrowCanceledOnApply { get; init; }
        internal bool ThrowHttpOnApply { get; init; }
        internal bool ThrowIoOnApply { get; init; }

        public Task<AppUpdateInfo?> CheckForUpdateAsync(System.Threading.CancellationToken cancellationToken = default)
        {
            if (ThrowCanceledOnCheck)
            {
                throw new OperationCanceledException("check canceled", cancellationToken);
            }

            if (ThrowOnCheck)
            {
                throw new InvalidOperationException("check failed");
            }

            return Task.FromResult(CheckResult);
        }

        public Task ApplyUpdateAsync(AppUpdateInfo update, System.Threading.CancellationToken cancellationToken = default)
        {
            if (ThrowCanceledOnApply)
            {
                throw new OperationCanceledException("apply canceled", cancellationToken);
            }

            if (ThrowHttpOnApply)
            {
                throw new HttpRequestException("network failed");
            }

            if (ThrowIoOnApply)
            {
                throw new IOException("io failed");
            }

            if (ThrowOnApply)
            {
                throw new InvalidOperationException("apply failed");
            }

            return Task.CompletedTask;
        }
    }
}
