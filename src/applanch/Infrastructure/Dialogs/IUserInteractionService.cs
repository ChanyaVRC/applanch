using System.Windows;

namespace applanch.Infrastructure.Dialogs;

internal interface IUserInteractionService
{
    void Show(string message, string caption, MessageBoxImage icon);
    bool Confirm(string message, string caption, Window owner);
    string? Prompt(string title, string initialValue, Window owner);
    string? PromptWithSuggestions(string title, string initialValue, IEnumerable<string> suggestions, Window owner);
}

