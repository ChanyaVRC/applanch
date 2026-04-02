using System.Windows;
using applanch.Infrastructure.Theming;
using Strings = applanch.Properties.Resources;

namespace applanch;

public sealed partial class ConfirmationDialogWindow : Window
{
    public ConfirmationDialogWindow(string message, string caption, Window owner)
    {
        InitializeComponent();

        Owner = owner;
        Title = caption;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;

        MessageText.Text = message;
        YesButton.Content = Strings.Confirm_Yes;
        NoButton.Content = Strings.Confirm_No;
    }

    private void Window_SourceInitialized(object? sender, EventArgs e) =>
        WindowCaptionThemeHelper.Apply(this);

    private void Window_Loaded(object sender, RoutedEventArgs e) =>
        NoButton.Focus();

    private void YesButton_Click(object sender, RoutedEventArgs e) =>
        DialogResult = true;

    private void NoButton_Click(object sender, RoutedEventArgs e) =>
        DialogResult = false;
}
