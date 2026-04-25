using System.Windows;
using applanch.Theming;

namespace applanch.Views.Dialogs;

public sealed partial class ConfirmationDialogWindow : DialogWindowBase
{
    public string DialogMessage { get; }

    public string YesButtonLabel { get; }

    public string NoButtonLabel { get; }

    public ConfirmationDialogWindow(string message, string caption, Window owner)
    {
        InitializeComponent();

        InitializeDialogWindow(caption, owner);

        DialogMessage = message;
        YesButtonLabel = AppResources.Confirm_Yes;
        NoButtonLabel = AppResources.Confirm_No;

        DataContext = this;
    }

    protected override void FocusInitialElement() =>
        NoButton.Focus();

    private void YesButton_Click(object sender, RoutedEventArgs e) =>
        DialogResult = true;

    private void NoButton_Click(object sender, RoutedEventArgs e) =>
        DialogResult = false;
}