using System.Windows;
using applanch.Infrastructure.Theming;
using Strings = applanch.Properties.Resources;

namespace applanch.Views.Dialogs;

public sealed partial class ConfirmationDialogWindow : Window
{
    public string DialogMessage { get; }

    public string YesButtonLabel { get; }

    public string NoButtonLabel { get; }

    public ConfirmationDialogWindow(string message, string caption, Window owner)
    {
        InitializeComponent();

        Owner = owner;
        Title = caption;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;

        DialogMessage = message;
        YesButtonLabel = Strings.Confirm_Yes;
        NoButtonLabel = Strings.Confirm_No;

        DataContext = this;
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
