using System.Windows;

namespace applanch.Infrastructure.Dialogs;

internal interface IUserInteractionService
{
    void Show(string message, string caption, MessageBoxImage icon);
    bool Confirm(string message, string caption, Window owner);
    string? Prompt(string title, string initialValue, Window owner);
    PromptResult<T?>? PromptWithSuggestions<T>(string title, T initialValue, IEnumerable<T> suggestions, Window owner);
}

