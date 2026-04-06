using System.Windows.Controls;
using System.Windows.Input;
using applanch.Infrastructure.Dialogs;
using applanch.Tests.TestSupport;
using applanch.ViewModels;
using Xunit;

namespace applanch.Tests.Infrastructure.Dialogs;

public class InlineRenameHandlerTests
{
    [Fact]
    public void HandleKeyDown_WithEnter_AppliesRenameAndConsumesEvent()
    {
        WpfTestHost.RunInSta(() =>
        {
            var item = new LaunchItemViewModel(new applanch.Infrastructure.Utilities.LaunchPath("path"), "Dev", string.Empty, "Old")
            {
                EditingName = "New",
                IsRenaming = true,
            };
            var textBox = new TextBox { DataContext = item };
            var sut = new InlineRenameHandler();

            LaunchItemViewModel? renamedItem = null;
            string? renamedValue = null;

            var handled = sut.HandleKeyDown(
                textBox,
                Key.Return,
                (target, value) =>
                {
                    renamedItem = target;
                    renamedValue = value;
                });

            Assert.True(handled);
            Assert.Same(item, renamedItem);
            Assert.Equal("New", renamedValue);
            Assert.False(item.IsRenaming);
        });
    }

    [Fact]
    public void HandleKeyDown_WithEscape_CancelsRenameAndConsumesEvent()
    {
        WpfTestHost.RunInSta(() =>
        {
            var item = new LaunchItemViewModel(new applanch.Infrastructure.Utilities.LaunchPath("path"), "Dev", string.Empty, "Old")
            {
                EditingName = "New",
                IsRenaming = true,
            };
            var textBox = new TextBox { DataContext = item };
            var sut = new InlineRenameHandler();

            var handled = sut.HandleKeyDown(textBox, Key.Escape, (_, _) => Assert.Fail("Should not apply rename on escape."));

            Assert.True(handled);
            Assert.False(item.IsRenaming);
        });
    }

    [Fact]
    public void HandleKeyDown_WithOtherKey_DoesNotConsumeEvent()
    {
        WpfTestHost.RunInSta(() =>
        {
            var item = new LaunchItemViewModel(new applanch.Infrastructure.Utilities.LaunchPath("path"), "Dev", string.Empty, "Old")
            {
                EditingName = "New",
                IsRenaming = true,
            };
            var textBox = new TextBox { DataContext = item };
            var sut = new InlineRenameHandler();

            var handled = sut.HandleKeyDown(textBox, Key.Tab, (_, _) => Assert.Fail("Should not apply rename on non-commit key."));

            Assert.False(handled);
            Assert.True(item.IsRenaming);
        });
    }

    [Fact]
    public void HandleLostFocus_WhenRenaming_AppliesRename()
    {
        WpfTestHost.RunInSta(() =>
        {
            var item = new LaunchItemViewModel(new applanch.Infrastructure.Utilities.LaunchPath("path"), "Dev", string.Empty, "Old")
            {
                EditingName = "New",
                IsRenaming = true,
            };
            var textBox = new TextBox { DataContext = item };
            var sut = new InlineRenameHandler();

            LaunchItemViewModel? renamedItem = null;
            string? renamedValue = null;

            sut.HandleLostFocus(
                textBox,
                (target, value) =>
                {
                    renamedItem = target;
                    renamedValue = value;
                });

            Assert.Same(item, renamedItem);
            Assert.Equal("New", renamedValue);
            Assert.False(item.IsRenaming);
        });
    }

    [Fact]
    public void HandleLostFocus_WhenNotRenaming_DoesNothing()
    {
        WpfTestHost.RunInSta(() =>
        {
            var item = new LaunchItemViewModel(new applanch.Infrastructure.Utilities.LaunchPath("path"), "Dev", string.Empty, "Old")
            {
                EditingName = "New",
                IsRenaming = false,
            };
            var textBox = new TextBox { DataContext = item };
            var sut = new InlineRenameHandler();

            sut.HandleLostFocus(textBox, (_, _) => Assert.Fail("Should not apply rename when not renaming."));

            Assert.False(item.IsRenaming);
        });
    }

}
