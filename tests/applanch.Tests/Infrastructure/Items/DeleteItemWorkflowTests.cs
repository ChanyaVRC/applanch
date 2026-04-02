using applanch.Infrastructure.Items;
using applanch.Infrastructure.Storage;
using applanch.ViewModels;
using Xunit;

namespace applanch.Tests.Infrastructure.Items;

public class DeleteItemWorkflowTests
{
    [Fact]
    public void TryDelete_ConfirmEnabledAndCancelled_ReturnsCancelledAndDoesNotRemove()
    {
        var workflow = new DeleteItemWorkflow();
        var settings = new AppSettings { ConfirmBeforeDelete = true };
        var items = new List<LaunchItemViewModel>
        {
            new(@"C:\\Tools\\app.exe", "Dev", string.Empty, "App")
        };
        var item = items[0];
        var removed = false;

        var result = workflow.TryDelete(item, settings, () => false, items, _ => removed = true);

        Assert.True(result.IsCancelled);
        Assert.False(removed);
        Assert.Equal(-1, result.DeletedIndex);
    }

    [Fact]
    public void TryDelete_Success_ReturnsOriginalIndexAndRemovesItem()
    {
        var workflow = new DeleteItemWorkflow();
        var settings = new AppSettings { ConfirmBeforeDelete = false };
        var items = new List<LaunchItemViewModel>
        {
            new(@"C:\\Tools\\a.exe", "Dev", string.Empty, "A"),
            new(@"C:\\Tools\\b.exe", "Dev", string.Empty, "B"),
            new(@"C:\\Tools\\c.exe", "Dev", string.Empty, "C")
        };
        var target = items[1];

        var result = workflow.TryDelete(target, settings, () => true, items, x => items.Remove(x));

        Assert.False(result.IsCancelled);
        Assert.Equal(1, result.DeletedIndex);
        Assert.Equal(["A", "C"], items.Select(x => x.DisplayName));
    }

    [Fact]
    public void TryDelete_ItemNotInList_ReturnsMinusOneIndex()
    {
        var workflow = new DeleteItemWorkflow();
        var settings = new AppSettings();
        var existing = new LaunchItemViewModel(@"C:\\Tools\\a.exe", "Dev", string.Empty, "A");
        var target = new LaunchItemViewModel(@"C:\\Tools\\x.exe", "Dev", string.Empty, "X");
        var items = new List<LaunchItemViewModel> { existing };

        var result = workflow.TryDelete(target, settings, () => true, items, x => items.Remove(x));

        Assert.False(result.IsCancelled);
        Assert.Equal(-1, result.DeletedIndex);
        Assert.Equal(["A"], items.Select(x => x.DisplayName));
    }
}
