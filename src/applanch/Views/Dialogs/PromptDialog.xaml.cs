using System.Windows;
using applanch.Infrastructure.Theming;

namespace applanch;

public sealed partial class PromptDialog : Window
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

    }

    private void Window_SourceInitialized(object? sender, EventArgs e) =>
        WindowCaptionThemeHelper.Apply(this);

    private void OkButton_Click(object sender, RoutedEventArgs e) =>
        DialogResult = true;

    private void Window_Loaded(object sender, RoutedEventArgs e)
    {
        if (_useSuggestions)
        {
            InputSuggestion.FocusInputWithoutAutoOpen(selectAll: true);
            return;
        }
        InputTextBox.Focus();
        InputTextBox.SelectAll();
    }

    public string InputValue => _useSuggestions
        ? InputSuggestion.Text?.Trim() ?? string.Empty
        : InputTextBox.Text.Trim();
}