using System.Windows;
using System.Windows.Controls;
using applanch.Controls;
using applanch.Tests.TestSupport;
using applanch.Views.Dialogs;
using Xunit;

namespace applanch.Tests.UI;

public sealed class PromptDialogUiTests
{
    [Fact]
    public void PromptDialog_WithSuggestions_ShowsSuggestionInputControl()
    {
        WpfTestHost.RunInSta(() =>
        {
            WpfTestHost.EnsureAppResources();

            using var owner = new TestWindow();
            owner.Show();

            var dialog = new PromptDialog("Change category", "Dev", owner, ["Dev", "Ops"]);
            dialog.Show();
            WpfTestHost.DoEvents();

            var inputTextBox = Assert.IsType<TextBox>(dialog.FindName("InputTextBox"));
            var inputSuggestion = Assert.IsType<SuggestionInputControl>(dialog.FindName("InputSuggestion"));

            Assert.True(dialog.UseSuggestions);
            Assert.Equal(Visibility.Collapsed, inputTextBox.Visibility);
            Assert.Equal(Visibility.Visible, inputSuggestion.Visibility);

            dialog.Close();
        });
    }

    [Fact]
    public void PromptDialog_WithoutSuggestions_ShowsTextBox()
    {
        WpfTestHost.RunInSta(() =>
        {
            WpfTestHost.EnsureAppResources();

            using var owner = new TestWindow();
            owner.Show();

            var dialog = new PromptDialog("Change arguments", "-flag", owner);
            dialog.Show();
            WpfTestHost.DoEvents();

            var inputTextBox = Assert.IsType<TextBox>(dialog.FindName("InputTextBox"));
            var inputSuggestion = Assert.IsType<SuggestionInputControl>(dialog.FindName("InputSuggestion"));

            Assert.False(dialog.UseSuggestions);
            Assert.Equal(Visibility.Visible, inputTextBox.Visibility);
            Assert.Equal(Visibility.Collapsed, inputSuggestion.Visibility);

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
