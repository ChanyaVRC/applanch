using System.Windows;
using System.Collections.Generic;
using System.Linq;

namespace applanch;

internal interface IUserInteractionService
{
    void Show(string message, string caption, MessageBoxImage icon);
    string? Prompt(string title, string initialValue, Window owner);
    string? PromptWithSuggestions(string title, string initialValue, IEnumerable<string> suggestions, Window owner);
}

internal sealed class UserInteractionService : IUserInteractionService
{
    public void Show(string message, string caption, MessageBoxImage icon)
    {
        var owner = ResolveOwnerWindow();
        var dialog = new MessageDialogWindow(message, caption, icon, owner);
        _ = dialog.ShowDialog();
    }

    public string? Prompt(string title, string initialValue, Window owner)
    {
        var dialog = new PromptDialog(title, initialValue, owner);
        return dialog.ShowDialog() == true ? dialog.InputValue : null;
    }

    public string? PromptWithSuggestions(string title, string initialValue, IEnumerable<string> suggestions, Window owner)
    {
        var dialog = new PromptDialog(title, initialValue, owner, suggestions);
        return dialog.ShowDialog() == true ? dialog.InputValue : null;
    }

    private static Window? ResolveOwnerWindow()
    {
        return Application.Current?.Windows
            .OfType<Window>()
            .FirstOrDefault(static window => window.IsActive)
            ?? Application.Current?.MainWindow;
    }
}
