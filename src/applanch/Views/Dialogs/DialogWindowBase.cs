using System.Windows;
using applanch.Infrastructure.Theming;

namespace applanch.Views.Dialogs;

public class DialogWindowBase : Window
{
    protected DialogWindowBase()
    {
        SourceInitialized += HandleSourceInitialized;
        Loaded += HandleLoaded;
    }

    protected void InitializeDialogWindow(string title, Window? owner)
    {
        Title = title;
        Owner = owner;
        WindowStartupLocation = owner is null
            ? WindowStartupLocation.CenterScreen
            : WindowStartupLocation.CenterOwner;
    }

    protected virtual void FocusInitialElement()
    {
    }

    private void HandleSourceInitialized(object? sender, EventArgs e) =>
        WindowCaptionThemeHelper.Apply(this);

    private void HandleLoaded(object sender, RoutedEventArgs e) =>
        FocusInitialElement();
}