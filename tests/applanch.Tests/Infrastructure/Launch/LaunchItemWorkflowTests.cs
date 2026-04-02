using applanch.Infrastructure.Launch;
using applanch.Infrastructure.Storage;
using applanch.Infrastructure.Utilities;
using applanch.ViewModels;
using System.Windows;
using Xunit;

namespace applanch.Tests.Infrastructure.Launch;

public class LaunchItemWorkflowTests
{
    [Fact]
    public void TryLaunch_ConfirmEnabledAndCancelled_ReturnsCancelled()
    {
        var launchService = new FakeItemLaunchService();
        var workflow = new LaunchItemWorkflow(launchService);
        var settings = new AppSettings { ConfirmBeforeLaunch = true };
        var item = new LaunchItemViewModel(@"C:\\Tools\\app.exe", "Dev", string.Empty, "App");

        var result = workflow.TryLaunch(item, settings, confirmLaunch: () => false);

        Assert.True(result.IsCancelled);
        Assert.False(launchService.Called);
    }

    [Fact]
    public void TryLaunch_LaunchFails_PropagatesFailureResult()
    {
        var launchService = new FakeItemLaunchService
        {
            Result = LaunchExecutionResult.Failed("boom", MessageBoxImage.Error)
        };
        var workflow = new LaunchItemWorkflow(launchService);
        var settings = new AppSettings();
        var item = new LaunchItemViewModel(@"C:\\Tools\\app.exe", "Dev", string.Empty, "App");

        var result = workflow.TryLaunch(item, settings, confirmLaunch: () => true);

        Assert.False(result.IsCancelled);
        Assert.False(result.Execution.IsSuccess);
        Assert.Equal("boom", result.Execution.Message);
    }

    [Fact]
    public void TryLaunch_Success_UsesConfiguredPostLaunchBehavior()
    {
        var launchService = new FakeItemLaunchService
        {
            Result = LaunchExecutionResult.Success()
        };
        var workflow = new LaunchItemWorkflow(launchService);
        var settings = new AppSettings
        {
            PostLaunchBehavior = PostLaunchBehavior.MinimizeWindow
        };
        var item = new LaunchItemViewModel(@"C:\\Tools\\app.exe", "Dev", string.Empty, "App");

        var result = workflow.TryLaunch(item, settings, confirmLaunch: () => true);

        Assert.False(result.IsCancelled);
        Assert.True(result.Execution.IsSuccess);
        Assert.Equal(PostLaunchBehavior.MinimizeWindow, result.PostLaunchBehavior);
        Assert.False(launchService.LastRunAsAdministrator);
    }

    [Fact]
    public void TryLaunch_RunAsAdministratorEnabled_PassesFlagToLaunchService()
    {
        var launchService = new FakeItemLaunchService
        {
            Result = LaunchExecutionResult.Success()
        };
        var workflow = new LaunchItemWorkflow(launchService);
        var settings = new AppSettings { RunAsAdministrator = true };
        var item = new LaunchItemViewModel(@"C:\\Tools\\app.exe", "Dev", string.Empty, "App");

        var result = workflow.TryLaunch(item, settings, confirmLaunch: () => true);

        Assert.True(result.Execution.IsSuccess);
        Assert.True(launchService.LastRunAsAdministrator);
    }

    private sealed class FakeItemLaunchService : IItemLaunchService
    {
        public bool Called { get; private set; }
        public bool LastRunAsAdministrator { get; private set; }
        public LaunchPath LastLaunchPath { get; private set; }
        public string LastArguments { get; private set; } = string.Empty;
        public LaunchExecutionResult Result { get; init; } = LaunchExecutionResult.Success();

        public LaunchExecutionResult TryLaunch(LaunchPath launchPath, string arguments, bool runAsAdministrator = false)
        {
            Called = true;
            LastLaunchPath = launchPath;
            LastArguments = arguments;
            LastRunAsAdministrator = runAsAdministrator;
            return Result;
        }
    }
}
