using System.Collections.Generic;
using System.Linq;
using System.Windows;

namespace applanch;

public partial class PromptDialog : Window
{
    private readonly bool _useSuggestions;

    public PromptDialog(string title, string initialValue, Window owner, IEnumerable<string>? suggestions = null)
    {
        InitializeComponent();
        Title = title;
        Owner = owner;

        var suggestionList = suggestions?
            .Where(static v => !string.IsNullOrWhiteSpace(v))
            .Distinct(StringComparer.Ordinal)
            .ToArray() ?? [];

        _useSuggestions = suggestionList.Length > 0;

        if (_useSuggestions)
        {
            InputTextBox.Visibility = Visibility.Collapsed;
            InputSuggestion.Visibility = Visibility.Visible;
            InputSuggestion.Suggestions = suggestionList;
            InputSuggestion.Text = initialValue;
        }
        else
        {
            InputTextBox.Text = initialValue;
        }

        SourceInitialized += (_, _) => WindowCaptionThemeHelper.Apply(this);

        OkButton.Click += (_, _) => DialogResult = true;
        Loaded += (_, _) =>
        {
            if (_useSuggestions)
            {
                InputSuggestion.FocusInputWithoutAutoOpen(selectAll: true);
                return;
            }
            InputTextBox.Focus();
            InputTextBox.SelectAll();
        };
    }

    public string InputValue => _useSuggestions
        ? InputSuggestion.Text?.Trim() ?? string.Empty
        : InputTextBox.Text.Trim();
}