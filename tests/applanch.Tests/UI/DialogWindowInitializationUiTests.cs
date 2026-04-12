using System.Windows;
using applanch.Tests.TestSupport;
using applanch.Views.Dialogs;
using Xunit;

namespace applanch.Tests.UI;

[Collection("WpfTests")]
public sealed class DialogWindowInitializationUiTests
{
    [Fact]
    public void MessageDialogWindow_WithoutOwner_CentersOnScreen()
    {
        WpfTestHost.RunInSta(() =>
        {
            WpfTestHost.EnsureAppResources();

            var dialog = new MessageDialogWindow("Saved.", "Status", MessageBoxImage.Information);

            Assert.Equal("Status", dialog.Title);
            Assert.Null(dialog.Owner);
            Assert.Equal(WindowStartupLocation.CenterScreen, dialog.WindowStartupLocation);

            dialog.Close();
        });
    }

    [Fact]
    public void PromptDialog_WithOwner_CentersOnOwner()
    {
        WpfTestHost.RunInSta(() =>
        {
            WpfTestHost.EnsureAppResources();

            using var owner = new TestWindow();
            WpfTestHost.ShowOffscreen(owner);
            var dialog = new PromptDialog("Rename", "Current", owner);

            Assert.Equal("Rename", dialog.Title);
            Assert.Same(owner, dialog.Owner);
            Assert.Equal(WindowStartupLocation.CenterOwner, dialog.WindowStartupLocation);

            dialog.Close();
        });
    }

    [Fact]
    public void ConfirmationDialogWindow_WithOwner_CentersOnOwner()
    {
        WpfTestHost.RunInSta(() =>
        {
            WpfTestHost.EnsureAppResources();

            using var owner = new TestWindow();
            WpfTestHost.ShowOffscreen(owner);
            var dialog = new ConfirmationDialogWindow("Delete item?", "Confirm", owner);

            Assert.Equal("Confirm", dialog.Title);
            Assert.Same(owner, dialog.Owner);
            Assert.Equal(WindowStartupLocation.CenterOwner, dialog.WindowStartupLocation);

            dialog.Close();
        });
    }

    private sealed class TestWindow : Window, IDisposable
    {
        public void Dispose()
        {
            if (IsVisible)
            {
                Close();
            }
        }
    }
}