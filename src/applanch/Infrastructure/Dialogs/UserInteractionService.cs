using System.Windows;
using applanch.Views.Dialogs;

namespace applanch.Infrastructure.Dialogs;

internal sealed class UserInteractionService : IUserInteractionService
{
    public UserInteractionService()
    {
    }

    public void Show(string message, string caption, MessageBoxImage icon)
    {
        var owner = ResolveOwnerWindow();
        var dialog = new MessageDialogWindow(message, caption, icon, owner);
        _ = dialog.ShowDialog();
    }


    public bool Confirm(string message, string caption, Window owner)
    {
        var dialog = new ConfirmationDialogWindow(message, caption, owner);
        return dialog.ShowDialog() == true;
    }

    public string? Prompt(string title, string initialValue, Window owner)
    {
        var dialog = new PromptDialog(title, initialValue, owner);
        return dialog.ShowDialog() == true ? dialog.Input.Text : null;
    }

    public PromptResult<T?>? PromptWithSuggestions<T>(string title, T initialValue, IEnumerable<T> suggestions, Window owner)
    {
        var dialog = new PromptDialog(title, initialValue, owner, suggestions.Cast<object?>());
        if (dialog.ShowDialog() != true)
        {
            return null;
        }

        var input = dialog.Input;
        return new PromptResult<T?>(input.Text, input.SelectedItem is T { } item ? item : default);
    }

    private static Window? ResolveOwnerWindow()
    {
        return Application.Current?.Windows
            .OfType<Window>()
            .FirstOrDefault(static window => window.IsActive)
            ?? Application.Current?.MainWindow;
    }
}

