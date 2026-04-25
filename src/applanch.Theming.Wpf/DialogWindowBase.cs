using System.Windows;

namespace applanch.Theming;

/// <summary>
/// Base class for WPF windows that automatically applies the application's caption theme.
/// </summary>
public class DialogWindowBase : Window
{
    protected DialogWindowBase()
    {
        SourceInitialized += HandleSourceInitialized;
        Loaded += HandleLoaded;
    }

    /// <summary>
    /// Configures the window title, owner, and startup position.
    /// </summary>
    protected void InitializeDialogWindow(string title, Window? owner)
    {
        Title = title;
        Owner = owner;
        WindowStartupLocation = owner is null
            ? WindowStartupLocation.CenterScreen
            : WindowStartupLocation.CenterOwner;
    }

    /// <summary>
    /// Override to set keyboard focus to an initial element when the window loads.
    /// </summary>
    protected virtual void FocusInitialElement()
    {
    }

    private void HandleSourceInitialized(object? sender, EventArgs e) =>
        WindowCaptionThemeHelper.Apply(this);

    private void HandleLoaded(object sender, RoutedEventArgs e) =>
        FocusInitialElement();
}
