using applanch.Tests.ViewModels.TestDoubles;
using applanch.ViewModels;
using Xunit;

namespace applanch.Tests.ViewModels;

public class QuickAddWorkflowTests
{
    [Fact]
    public void TryCreateLaunchItem_EmptyInput_ReturnsInformationFailure()
    {
        var workflow = new QuickAddWorkflow(new FakeResolver());

        var result = workflow.TryCreateLaunchItem("   ", "Dev", string.Empty, [], out var newItem);

        Assert.False(result.IsSuccess);
        Assert.Equal(QuickAddMessageSeverity.Information, result.Severity);
        Assert.Null(newItem);
    }

    [Fact]
    public void TryCreateLaunchItem_UnresolvedInput_ReturnsWarningFailure()
    {
        var workflow = new QuickAddWorkflow(new FakeResolver());

        var result = workflow.TryCreateLaunchItem("unknown", "Dev", string.Empty, [], out var newItem);

        Assert.False(result.IsSuccess);
        Assert.Equal(QuickAddMessageSeverity.Warning, result.Severity);
        Assert.Null(newItem);
    }

    [Theory]
    [InlineData(@"C:/Tools/App.exe")]
    [InlineData(@"c:/tools/app.exe")]
    [InlineData(@"D:/")]
    public void TryCreateLaunchItem_ForwardSlashAbsolutePath_ReturnsWarningFailure(string input)
    {
        var workflow = new QuickAddWorkflow(new FakeResolver());

        var result = workflow.TryCreateLaunchItem(input, "Dev", string.Empty, [], out var newItem);

        Assert.False(result.IsSuccess);
        Assert.Equal(QuickAddMessageSeverity.Warning, result.Severity);
        Assert.Null(newItem);
    }

    [Fact]
    public void TryCreateLaunchItem_DuplicatePath_ReturnsInformationFailure()
    {
        var existingPath = @"C:\\Tools\\App.exe";
        var resolver = new FakeResolver
        {
            ShouldResolve = true,
            ResolvedApp = new applanch.Infrastructure.Resolution.ResolvedApp(existingPath, "App"),
        };
        var workflow = new QuickAddWorkflow(resolver);
        var existingItems =
            new[] { new LaunchItemViewModel(existingPath, "Dev", string.Empty, "App") };

        var result = workflow.TryCreateLaunchItem("app", "Dev", string.Empty, existingItems, out var newItem);

        Assert.False(result.IsSuccess);
        Assert.Equal(QuickAddMessageSeverity.Information, result.Severity);
        Assert.Null(newItem);
    }

    [Fact]
    public void TryCreateLaunchItem_DuplicatePathWithDifferentNotation_ReturnsInformationFailure()
    {
        var existingPath = @"C:\\Tools\\App.exe";
        var resolver = new FakeResolver
        {
            ShouldResolve = true,
            ResolvedApp = new applanch.Infrastructure.Resolution.ResolvedApp(@"C:\\Tools\\.\\App.exe", "App"),
        };
        var workflow = new QuickAddWorkflow(resolver);
        var existingItems =
            new[] { new LaunchItemViewModel(existingPath, "Dev", string.Empty, "App") };

        var result = workflow.TryCreateLaunchItem("app", "Dev", string.Empty, existingItems, out var newItem);

        Assert.False(result.IsSuccess);
        Assert.Equal(QuickAddMessageSeverity.Information, result.Severity);
        Assert.Null(newItem);
    }

    [Fact]
    public void TryCreateLaunchItem_DuplicatePathWithDifferentDriveLetterCase_ReturnsInformationFailure()
    {
        var existingPath = @"C:\\Tools\\App.exe";
        var resolver = new FakeResolver
        {
            ShouldResolve = true,
            ResolvedApp = new applanch.Infrastructure.Resolution.ResolvedApp(@"c:\\Tools\\App.exe", "App"),
        };
        var workflow = new QuickAddWorkflow(resolver);
        var existingItems =
            new[] { new LaunchItemViewModel(existingPath, "Dev", string.Empty, "App") };

        var result = workflow.TryCreateLaunchItem("app", "Dev", string.Empty, existingItems, out var newItem);

        Assert.False(result.IsSuccess);
        Assert.Equal(QuickAddMessageSeverity.Information, result.Severity);
        Assert.Null(newItem);
    }

    [Fact]
    public void TryCreateLaunchItem_DuplicateDirectoryPathWithTrailingSeparator_ReturnsInformationFailure()
    {
        var existingPath = @"C:\\Tools\\Folder";
        var resolver = new FakeResolver
        {
            ShouldResolve = true,
            ResolvedApp = new applanch.Infrastructure.Resolution.ResolvedApp(@"C:\\Tools\\Folder\\", "Folder"),
        };
        var workflow = new QuickAddWorkflow(resolver);
        var existingItems =
            new[] { new LaunchItemViewModel(existingPath, "Dev", string.Empty, "Folder") };

        var result = workflow.TryCreateLaunchItem("folder", "Dev", string.Empty, existingItems, out var newItem);

        Assert.False(result.IsSuccess);
        Assert.Equal(QuickAddMessageSeverity.Information, result.Severity);
        Assert.Null(newItem);
    }

    [Fact]
    public void TryCreateLaunchItem_Success_ReturnsCreatedItem()
    {
        var resolver = new FakeResolver
        {
            ShouldResolve = true,
            ResolvedApp = new applanch.Infrastructure.Resolution.ResolvedApp(@"C:\\Tools\\NewApp.exe", "NewApp"),
        };
        var workflow = new QuickAddWorkflow(resolver);

        var result = workflow.TryCreateLaunchItem("newapp", "Ops", "-v", [], out var newItem);

        Assert.True(result.IsSuccess);
        Assert.NotNull(newItem);
        Assert.Equal("NewApp", newItem.DisplayName);
        Assert.Equal("Ops", newItem.Category);
        Assert.Equal("-v", newItem.Arguments);
    }

    [Fact]
    public void TryCreateLaunchItem_Success_NormalizesResolvedForwardSlashPath()
    {
        var resolver = new FakeResolver
        {
            ShouldResolve = true,
            ResolvedApp = new applanch.Infrastructure.Resolution.ResolvedApp(@"C:/Tools/NewApp.exe", "NewApp"),
        };
        var workflow = new QuickAddWorkflow(resolver);

        var result = workflow.TryCreateLaunchItem("newapp", "Ops", "-v", [], out var newItem);

        Assert.True(result.IsSuccess);
        Assert.NotNull(newItem);
        Assert.Equal(@"C:\Tools\NewApp.exe", newItem.FullPath);
    }

    [Fact]
    public void GetSuggestions_DelegatesToResolver()
    {
        var resolver = new FakeResolver
        {
            SuggestionsOverride = ["a", "b", "c"],
        };
        var workflow = new QuickAddWorkflow(resolver);

        var result = workflow.GetSuggestions("x", 2);

        Assert.Equal(new[] { "a", "b" }, result);
        Assert.Equal(1, resolver.SuggestionsCallCount);
    }
}
