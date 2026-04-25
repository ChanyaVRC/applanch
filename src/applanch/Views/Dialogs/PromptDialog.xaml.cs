using System.Windows;
using applanch.Infrastructure.Dialogs;
using applanch.Theming;

namespace applanch.Views.Dialogs;

public sealed partial class PromptDialog : DialogWindowBase
{
    public bool UseSuggestions => SuggestionTexts.Count > 0;

    public IReadOnlyList<object?> Suggestions { get; }

    public IReadOnlyList<string> SuggestionTexts { get; }

    public string InitialValue { get; }

    public PromptDialog(string title, object? initialValue, Window owner, IEnumerable<object?>? suggestions = null)
    {
        if (suggestions is null || !suggestions.Any())
        {
            Suggestions = Array.Empty<object?>();
            SuggestionTexts = Array.Empty<string>();
        }
        else
        {
            var seenTexts = new HashSet<string>(StringComparer.Ordinal);
            var normalizedItems = new List<object?>();
            var normalizedTexts = new List<string>();

            foreach (var value in suggestions)
            {
                var text = GetPromptText(value);
                if (string.IsNullOrWhiteSpace(text) || !seenTexts.Add(text))
                {
                    continue;
                }

                normalizedItems.Add(value);
                normalizedTexts.Add(text);
            }

            Suggestions = normalizedItems;
            SuggestionTexts = normalizedTexts;
        }

        InitialValue = GetPromptText(initialValue);

        InitializeComponent();
        InitializeDialogWindow(title, owner);
        DataContext = this;
    }

    private void OkButton_Click(object sender, RoutedEventArgs e) =>
        DialogResult = true;

    protected override void FocusInitialElement()
    {
        if (UseSuggestions)
        {
            InputSuggestion.FocusInputWithoutAutoOpen(selectAll: true);
            return;
        }

        InputTextBox.Focus();
        InputTextBox.SelectAll();
    }

    public PromptResult<object?> Input
    {
        get
        {
            var text = UseSuggestions
                ? InputSuggestion.Text?.Trim() ?? string.Empty
                : InputTextBox.Text.Trim();
            var selectedSuggestion = ResolveSelectedSuggestion(text);
            return new PromptResult<object?>(text, selectedSuggestion);
        }
    }

    public string InputValue => Input.Text;

    private static string GetPromptText(object? value)
    {
        return value?.ToString() ?? string.Empty;
    }

    private object? ResolveSelectedSuggestion(string text)
    {
        if (!UseSuggestions)
        {
            return null;
        }

        for (var i = 0; i < SuggestionTexts.Count; i++)
        {
            if (string.Equals(SuggestionTexts[i], text, StringComparison.Ordinal))
            {
                return Suggestions[i];
            }
        }

        return null;
    }
}