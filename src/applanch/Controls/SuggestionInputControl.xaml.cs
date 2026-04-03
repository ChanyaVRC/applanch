using System.Collections;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Threading;
using applanch.Infrastructure.Utilities;

namespace applanch.Controls;

public sealed partial class SuggestionInputControl : UserControl
{
    private static readonly string[] EmptySuggestions = [];

    private bool _suppressing;
    private bool _inputHandlersAttached;
    private bool _openingFromTyping;
    private bool _showAllSuggestions;
    private bool _userEditedWhileOpen;
    private bool _deferAutoOpenUntilUserInput;

    public SuggestionInputControl()
    {
        InitializeComponent();
    }

    public static readonly DependencyProperty TextProperty =
        DependencyProperty.Register(
            nameof(Text),
            typeof(string),
            typeof(SuggestionInputControl),
            new FrameworkPropertyMetadata(
                string.Empty,
                FrameworkPropertyMetadataOptions.BindsTwoWayByDefault,
                static (d, _) => ((SuggestionInputControl)d).OnTextChanged()));

    public static readonly DependencyProperty SuggestionsProperty =
        DependencyProperty.Register(
            nameof(Suggestions),
            typeof(IEnumerable),
            typeof(SuggestionInputControl),
            new PropertyMetadata(null, static (d, _) => ((SuggestionInputControl)d).OnSuggestionsChanged()));

    public static readonly DependencyProperty InputComboBoxStyleProperty =
        DependencyProperty.Register(
            nameof(InputComboBoxStyle),
            typeof(Style),
            typeof(SuggestionInputControl),
            new PropertyMetadata(null));

    private static readonly DependencyPropertyKey FilteredSuggestionsPropertyKey =
        DependencyProperty.RegisterReadOnly(
            nameof(FilteredSuggestions),
            typeof(IEnumerable),
            typeof(SuggestionInputControl),
            new PropertyMetadata(EmptySuggestions));

    public static readonly DependencyProperty FilteredSuggestionsProperty = FilteredSuggestionsPropertyKey.DependencyProperty;

    public string Text
    {
        get => (string)GetValue(TextProperty);
        set => SetValue(TextProperty, value);
    }

    public IEnumerable? Suggestions
    {
        get => (IEnumerable?)GetValue(SuggestionsProperty);
        set => SetValue(SuggestionsProperty, value);
    }

    public Style? InputComboBoxStyle
    {
        get => (Style?)GetValue(InputComboBoxStyleProperty);
        set => SetValue(InputComboBoxStyleProperty, value);
    }

    public IEnumerable FilteredSuggestions => (IEnumerable)GetValue(FilteredSuggestionsProperty);

    public void FocusInput(bool selectAll)
    {
        if (InputComboBox is null)
        {
            return;
        }

        InputComboBox.Focus();

        if (selectAll && GetEditableTextBox(InputComboBox) is TextBox textBox)
        {
            textBox.Dispatcher.BeginInvoke(() =>
            {
                textBox.Focus();
                textBox.SelectAll();
            }, DispatcherPriority.Input);
        }
    }

    public void FocusInputWithoutAutoOpen(bool selectAll)
    {
        _deferAutoOpenUntilUserInput = true;
        FocusInput(selectAll);
    }

    private void SuggestionInputControl_Loaded(object sender, RoutedEventArgs e)
    {
        AttachInputHandlers();
        SyncTextFromEditableTextBox();
    }

    private void SuggestionInputControl_Unloaded(object sender, RoutedEventArgs e)
    {
        DetachInputHandlers();
    }

    private void AttachInputHandlers()
    {
        if (_inputHandlersAttached || InputComboBox is null)
        {
            return;
        }

        InputComboBox.AddHandler(TextBoxBase.TextChangedEvent, new TextChangedEventHandler(EditableTextBox_TextChanged), true);
        InputComboBox.AddHandler(TextCompositionManager.TextInputUpdateEvent, new TextCompositionEventHandler(EditableTextComposition_Updated), true);
        InputComboBox.DropDownOpened += InputComboBox_DropDownOpened;
        InputComboBox.DropDownClosed += InputComboBox_DropDownClosed;
        _inputHandlersAttached = true;
    }

    private void DetachInputHandlers()
    {
        if (!_inputHandlersAttached || InputComboBox is null)
        {
            return;
        }

        InputComboBox.RemoveHandler(TextBoxBase.TextChangedEvent, new TextChangedEventHandler(EditableTextBox_TextChanged));
        InputComboBox.RemoveHandler(TextCompositionManager.TextInputUpdateEvent, new TextCompositionEventHandler(EditableTextComposition_Updated));
        InputComboBox.DropDownOpened -= InputComboBox_DropDownOpened;
        InputComboBox.DropDownClosed -= InputComboBox_DropDownClosed;
        _inputHandlersAttached = false;
    }

    private void InputComboBox_DropDownOpened(object? sender, EventArgs e)
    {
        _showAllSuggestions = !_openingFromTyping;
        _openingFromTyping = false;
        _userEditedWhileOpen = false;

        RefreshSuggestionFilter();
        SyncTextFromEditableTextBox();
    }

    private void InputComboBox_DropDownClosed(object? sender, EventArgs e)
    {
        _openingFromTyping = false;
        _showAllSuggestions = false;
        var userEdited = _userEditedWhileOpen;
        _userEditedWhileOpen = false;

        if (_suppressing || InputComboBox is null)
        {
            return;
        }

        if (!userEdited
            && InputComboBox.SelectedItem is string selectedText
            && !string.IsNullOrWhiteSpace(selectedText))
        {
            ApplySelectedText(InputComboBox, selectedText);
            return;
        }

        // Preserve user text.
        var text = Text ?? InputComboBox.Text;
        InputComboBox.SelectedItem = null;
        InputComboBox.Text = text;
        Text = text;
    }

    private void EditableTextBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (e.Changes.Count > 0)
        {
            RegisterUserInput();
        }

        SyncTextFromEditableTextBox();
    }

    private void EditableTextComposition_Updated(object sender, TextCompositionEventArgs e)
    {
        RegisterUserInput();
        // IME composition text can lag one dispatcher turn behind TextChanged.
        Dispatcher.BeginInvoke(SyncTextFromEditableTextBox, DispatcherPriority.Input);
    }

    private void SyncTextFromEditableTextBox()
    {
        if (InputComboBox is null || GetEditableTextBox(InputComboBox) is not TextBox textBox)
        {
            return;
        }

        if (Text != textBox.Text)
        {
            Text = textBox.Text;
            return;
        }

        // Text unchanged — still refresh for IME conversion phase transitions.
        RefreshSuggestionFilter();
        UpdateDropDownVisibility();
    }

    private void ComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_suppressing || sender is not ComboBox comboBox || comboBox.IsDropDownOpen)
        {
            return;
        }

        // WPF auto-selected a matching item — clear it to keep text editable.
        if (e.AddedItems.Count > 0)
        {
            var currentText = Text;
            _suppressing = true;
            try
            {
                comboBox.SelectedItem = null;
                comboBox.Text = currentText;
                Text = currentText;
            }
            finally
            {
                _suppressing = false;
            }
        }
    }

    private void ComboBox_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        if (sender is not ComboBox comboBox || e.OriginalSource is not DependencyObject source)
        {
            return;
        }

        if (VisualTreeUtilities.FindAncestor<ComboBoxItem>(source)?.DataContext is string selectedText
            && !string.IsNullOrWhiteSpace(selectedText))
        {
            ApplySelectedText(comboBox, selectedText);
        }
    }

    private void OnTextChanged()
    {
        RefreshSuggestionFilter();
        UpdateDropDownVisibility();
    }

    private void OnSuggestionsChanged()
    {
        RefreshSuggestionFilter();
        UpdateDropDownVisibility();
    }

    private void UpdateDropDownVisibility()
    {
        if (_suppressing || !IsLoaded || InputComboBox is null)
        {
            return;
        }

        if (!HasVisibleSuggestions())
        {
            InputComboBox.IsDropDownOpen = false;
            return;
        }

        if (string.IsNullOrWhiteSpace(Text) || _deferAutoOpenUntilUserInput)
        {
            return;
        }

        if (InputComboBox.IsKeyboardFocusWithin || InputComboBox.IsFocused)
        {
            MoveCaretToEnd(InputComboBox);
            _openingFromTyping = true;
            InputComboBox.IsDropDownOpen = true;
        }
    }

    private static TextBox? GetEditableTextBox(ComboBox comboBox)
    {
        return comboBox.Template?.FindName("PART_EditableTextBox", comboBox) as TextBox;
    }

    private void ApplySelectedText(ComboBox comboBox, string selectedText)
    {
        _suppressing = true;
        try
        {
            Text = selectedText;
            comboBox.Text = selectedText;
            comboBox.IsDropDownOpen = false;
        }
        finally
        {
            _suppressing = false;
        }

        RefreshSuggestionFilter();

        comboBox.Dispatcher.BeginInvoke(() =>
        {
            if (GetEditableTextBox(comboBox) is not TextBox textBox)
            {
                return;
            }

            textBox.Focus();
            textBox.SelectionLength = 0;
            textBox.CaretIndex = textBox.Text.Length;
        }, DispatcherPriority.Render);
    }

    private void RegisterUserInput()
    {
        _deferAutoOpenUntilUserInput = false;
        _showAllSuggestions = false;

        if (InputComboBox is { IsDropDownOpen: true })
        {
            _userEditedWhileOpen = true;
        }
    }

    private static void MoveCaretToEnd(ComboBox comboBox)
    {
        if (GetEditableTextBox(comboBox) is not TextBox textBox)
        {
            return;
        }

        // Run after WPF applies its own selection update.
        textBox.Dispatcher.BeginInvoke(() =>
        {
            textBox.SelectionLength = 0;
            textBox.CaretIndex = textBox.Text?.Length ?? 0;
        }, DispatcherPriority.Input);
    }

    private bool HasVisibleSuggestions()
    {
        var enumerator = FilteredSuggestions.GetEnumerator();
        using var disposable = enumerator as IDisposable;
        return enumerator.MoveNext();
    }

    private void RefreshSuggestionFilter()
    {
        if (_suppressing)
        {
            return;
        }

        // Always clear SelectedItem to prevent WPF text-blanking cascade on ItemsSource change.
        if (InputComboBox is { SelectedItem: not null })
        {
            var text = Text ?? string.Empty;
            _suppressing = true;
            try
            {
                InputComboBox.SelectedItem = null;
                Text = text;
                InputComboBox.Text = text;
            }
            finally
            {
                _suppressing = false;
            }
        }

        SetValue(FilteredSuggestionsPropertyKey, BuildFilteredSuggestions());
    }

    private string[] BuildFilteredSuggestions()
    {
        if (Suggestions is null)
        {
            return EmptySuggestions;
        }

        var items = Suggestions.Cast<object>().OfType<string>()
            .Where(static s => !string.IsNullOrWhiteSpace(s));

        if (_showAllSuggestions)
        {
            return items.ToArray();
        }

        var keyword = Text?.Trim() ?? string.Empty;
        if (string.IsNullOrEmpty(keyword))
        {
            return EmptySuggestions;
        }

        return items
            .Where(s => s.Contains(keyword, StringComparison.OrdinalIgnoreCase))
            .ToArray();
    }

}
