using System.Windows;
using System.Collections.Generic;

namespace applanch.Infrastructure.Dialogs;

internal interface IUserInteractionService
{
    void Show(string message, string caption, MessageBoxImage icon);
    string? Prompt(string title, string initialValue, Window owner);
    string? PromptWithSuggestions(string title, string initialValue, IEnumerable<string> suggestions, Window owner);
}

