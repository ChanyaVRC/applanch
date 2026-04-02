using System.Windows;
using applanch.Views.Dialogs;

namespace applanch.Infrastructure.Dialogs;

internal sealed class UserInteractionService : IUserInteractionService
{
    private readonly Func<string, string, Window, bool?> _confirmDialogInvoker;

    public UserInteractionService()
        : this((message, caption, owner) => new ConfirmationDialogWindow(message, caption, owner).ShowDialog())
    {
    }

    internal UserInteractionService(Func<string, string, Window, bool?> confirmDialogInvoker)
    {
        _confirmDialogInvoker = confirmDialogInvoker;
    }

    public void Show(string message, string caption, MessageBoxImage icon)
    {
        var owner = ResolveOwnerWindow();
        var dialog = new MessageDialogWindow(message, caption, icon, owner);
        _ = dialog.ShowDialog();
    }

    public bool Confirm(string message, string caption, Window owner)
    {
        return _confirmDialogInvoker(message, caption, owner) == true;
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

