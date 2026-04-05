using System.Windows;
using applanch.Infrastructure.Theming;

namespace applanch.Views.Dialogs;

public sealed partial class PromptDialog : Window
{
    public bool UseSuggestions { get; }

    public IReadOnlyList<string> Suggestions { get; }

    public string InitialValue { get; }

    public PromptDialog(string title, string initialValue, Window owner, IEnumerable<string>? suggestions = null)
    {
        var suggestionList = suggestions?
            .Where(static v => !string.IsNullOrWhiteSpace(v))
            .Distinct(StringComparer.Ordinal)
            .ToArray() ?? [];
        Suggestions = suggestionList;
        UseSuggestions = suggestionList.Length > 0;
        InitialValue = initialValue;

        InitializeComponent();
        Title = title;
        Owner = owner;
        DataContext = this;
    }


    private void Window_SourceInitialized(object? sender, EventArgs e) =>
        WindowCaptionThemeHelper.Apply(this);

    private void OkButton_Click(object sender, RoutedEventArgs e) =>
        DialogResult = true;

    private void Window_Loaded(object sender, RoutedEventArgs e)
    {
        if (UseSuggestions)
        {
            InputSuggestion.FocusInputWithoutAutoOpen(selectAll: true);
            return;
        }
        InputTextBox.Focus();
        InputTextBox.SelectAll();
    }

    public string InputValue => UseSuggestions
        ? InputSuggestion.Text?.Trim() ?? string.Empty
        : InputTextBox.Text.Trim();
}