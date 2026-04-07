using applanch.Infrastructure.Presentation;
using Xunit;

namespace applanch.Tests.Infrastructure.Presentation;

public sealed class CategorySidebarStateControllerTests
{
    [Fact]
    public void Constructor_StartsPinnedAndExpanded()
    {
        var sut = new CategorySidebarStateController();

        Assert.True(sut.IsPinned);
        Assert.True(sut.IsExpanded);
    }

    [Fact]
    public void SetPinned_FalseWithoutPointer_CollapsesImmediately()
    {
        var sut = new CategorySidebarStateController();

        var changed = sut.SetPinned(false);

        Assert.True(changed);
        Assert.False(sut.IsPinned);
        Assert.False(sut.IsExpanded);
    }

    [Fact]
    public void UnpinnedSidebar_ExpandsOnPointerEnter_AndCollapsesAfterLeave()
    {
        var sut = new CategorySidebarStateController();
        sut.SetPinned(false);

        var expanded = sut.HandleTriggerEntered();

        Assert.True(expanded);
        Assert.True(sut.IsExpanded);

        sut.HandleTriggerExited();

        Assert.True(sut.TryCollapse());
        Assert.False(sut.IsExpanded);
    }

    [Fact]
    public void UnpinnedSidebar_StaysExpandedWhilePointerIsOverSidebar()
    {
        var sut = new CategorySidebarStateController();
        sut.SetPinned(false);
        sut.HandleTriggerEntered();
        sut.HandleTriggerExited();
        sut.HandleSidebarEntered();

        Assert.False(sut.TryCollapse());
        Assert.True(sut.IsExpanded);
    }

    [Fact]
    public void SetPinned_TrueKeepsSidebarExpanded()
    {
        var sut = new CategorySidebarStateController();
        sut.SetPinned(false);

        var changed = sut.SetPinned(true);

        Assert.True(changed);
        Assert.True(sut.IsPinned);
        Assert.True(sut.IsExpanded);
    }
}