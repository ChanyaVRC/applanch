using System.Windows.Controls;
using System.Windows.Input;
using applanch.Infrastructure.Storage;
using applanch.Tests.TestSupport;
using applanch.ViewModels;
using applanch.Workflows.Items;
using Xunit;

namespace applanch.Tests.Workflows.Items;

[Collection("WpfTests")]
public class InlineRenameHandlerTests
{
    [Fact]
    public void HandleKeyDown_WithEnter_AppliesRenameAndConsumesEvent()
    {
        WpfTestHost.RunInSta(() =>
        {
            var item = new LaunchItemViewModel(new LaunchPath("path"), Category.FromInput("Dev"), string.Empty, "Old")
            {
                EditingName = "New",
                IsRenaming = true,
            };
            var textBox = new TextBox { DataContext = item };
            var sut = new InlineRenameHandler();

            var handled = sut.HandleKeyDown(textBox, Key.Return);

            Assert.True(handled);
            Assert.Equal("New", item.DisplayName);
            Assert.False(item.IsRenaming);
        });
    }

    [Fact]
    public void HandleKeyDown_WithEscape_CancelsRenameAndConsumesEvent()
    {
        WpfTestHost.RunInSta(() =>
        {
            var item = new LaunchItemViewModel(new LaunchPath("path"), Category.FromInput("Dev"), string.Empty, "Old")
            {
                EditingName = "New",
                IsRenaming = true,
            };
            var textBox = new TextBox { DataContext = item };
            var sut = new InlineRenameHandler();

            var handled = sut.HandleKeyDown(textBox, Key.Escape);

            Assert.True(handled);
            Assert.Equal("Old", item.DisplayName);
            Assert.False(item.IsRenaming);
        });
    }

    [Fact]
    public void HandleKeyDown_WithOtherKey_DoesNotConsumeEvent()
    {
        WpfTestHost.RunInSta(() =>
        {
            var item = new LaunchItemViewModel(new LaunchPath("path"), Category.FromInput("Dev"), string.Empty, "Old")
            {
                EditingName = "New",
                IsRenaming = true,
            };
            var textBox = new TextBox { DataContext = item };
            var sut = new InlineRenameHandler();

            var handled = sut.HandleKeyDown(textBox, Key.Tab);

            Assert.False(handled);
            Assert.Equal("Old", item.DisplayName);
            Assert.True(item.IsRenaming);
        });
    }

    [Fact]
    public void HandleLostFocus_WhenRenaming_AppliesRename()
    {
        WpfTestHost.RunInSta(() =>
        {
            var item = new LaunchItemViewModel(new LaunchPath("path"), Category.FromInput("Dev"), string.Empty, "Old")
            {
                EditingName = "New",
                IsRenaming = true,
            };
            var textBox = new TextBox { DataContext = item };
            var sut = new InlineRenameHandler();

            sut.HandleLostFocus(textBox);

            Assert.Equal("New", item.DisplayName);
            Assert.False(item.IsRenaming);
        });
    }

    [Fact]
    public void HandleLostFocus_WhenNotRenaming_DoesNothing()
    {
        WpfTestHost.RunInSta(() =>
        {
            var item = new LaunchItemViewModel(new LaunchPath("path"), Category.FromInput("Dev"), string.Empty, "Old")
            {
                EditingName = "New",
                IsRenaming = false,
            };
            var textBox = new TextBox { DataContext = item };
            var sut = new InlineRenameHandler();

            sut.HandleLostFocus(textBox);

            Assert.Equal("Old", item.DisplayName);
            Assert.False(item.IsRenaming);
        });
    }

}
