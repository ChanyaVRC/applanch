using System.Windows;
using applanch.Infrastructure.Dialogs;

namespace applanch.Tests.ViewModels.TestDoubles;

internal sealed class FakeInteractionService : IUserInteractionService
{
    private string? _promptValue;

    public FakeInteractionService()
    {
    }

    public FakeInteractionService(string? promptValue)
    {
        _promptValue = promptValue;
    }

    public string? PromptResult { get; set; }
    public string? PromptWithSuggestionsResult { get; set; }
    public string LastPromptWithSuggestionsTitle { get; private set; } = string.Empty;
    public string[] LastSuggestions { get; private set; } = [];
    public int PromptCallCount { get; private set; }

    public void Show(string message, string caption, MessageBoxImage icon)
    {
    }

    public bool Confirm(string message, string caption, Window owner)
    {
        return true;
    }

    public string? Prompt(string title, string initialValue, Window owner)
    {
        PromptCallCount++;
        return PromptResult ?? _promptValue ?? initialValue;
    }

    public PromptResult<string>? PromptWithSuggestions(string title, string initialValue, IEnumerable<string> suggestions, Window owner)
    {
        LastPromptWithSuggestionsTitle = title;
        LastSuggestions = suggestions
            .Where(static value => !string.IsNullOrWhiteSpace(value))
            .Distinct(StringComparer.Ordinal)
            .ToArray();

        var result = PromptWithSuggestionsResult;
        if (result is null)
        {
            return null;
        }

        var selectedSuggestion = LastSuggestions.FirstOrDefault(value => string.Equals(value, result, StringComparison.Ordinal));
        return new PromptResult<string>(result, selectedSuggestion);
    }

    public PromptResult<T?>? PromptWithSuggestions<T>(string title, T initialValue, IEnumerable<T> suggestions, Window owner)
    {
        LastPromptWithSuggestionsTitle = title;
        LastSuggestions = suggestions
            .Select(static value => value?.ToString() ?? string.Empty)
            .Where(static value => !string.IsNullOrWhiteSpace(value))
            .Distinct(StringComparer.Ordinal)
            .ToArray();

        var result = PromptWithSuggestionsResult;
        if (result is null)
        {
            return null;
        }

        var selectedSuggestion = LastSuggestions.FirstOrDefault(value => string.Equals(value, result, StringComparison.Ordinal));
        var selectedItem = suggestions.FirstOrDefault(value => string.Equals(value?.ToString(), selectedSuggestion, StringComparison.Ordinal));
        return new PromptResult<T?>(result, selectedItem);
    }
}
