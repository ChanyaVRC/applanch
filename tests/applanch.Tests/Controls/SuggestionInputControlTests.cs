using System.Collections;
using System.Reflection;
using System.Runtime.ExceptionServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Threading;
using applanch.Controls;
using Xunit;

namespace applanch.Tests.Controls;

public class SuggestionInputControlTests
{
    [Fact]
    public void ComboBoxSelectionChanged_WithValidSelection_PreservesExistingText()
    {
        RunInSta(() =>
        {
            var control = new SuggestionInputControl { Text = "notepad" };
            var combo = new ComboBox { Text = "notepad" };
            var args = new SelectionChangedEventArgs(
                Selector.SelectionChangedEvent,
                new ArrayList(),
                new ArrayList { "notepad" });

            InvokeSelectionChanged(control, combo, args);

            // SelectionChanged should preserve text, not overwrite it
            Assert.Equal("notepad", control.Text);
            Assert.Equal("notepad", combo.Text);
        });
    }

    [Fact]
    public void TextProperty_CanBeSet_And_Retrieved()
    {
        RunInSta(() =>
        {
            var control = new SuggestionInputControl();
            var testText = "firefox";

            control.Text = testText;

            Assert.Equal(testText, control.Text);
        });
    }

    [Fact]
    public void TextProperty_Multiple_States()
    {
        RunInSta(() =>
        {
            var control = new SuggestionInputControl();

            // Empty string
            control.Text = string.Empty;
            Assert.Equal(string.Empty, control.Text);

            // Normal text
            control.Text = "first";
            Assert.Equal("first", control.Text);

            // Text with spaces
            control.Text = "   spaces   ";
            Assert.Equal("   spaces   ", control.Text);

            // Long path
            var longText = "C:\\Program Files\\Mozilla Firefox\\firefox.exe";
            control.Text = longText;
            Assert.Equal(longText, control.Text);

            // Special characters
            var specialText = "test-file_2024.exe";
            control.Text = specialText;
            Assert.Equal(specialText, control.Text);
        });
    }

    [Fact]
    public void SuggestionsProperty_And_FilteredSuggestions()
    {
        RunInSta(() =>
        {
            var control = new SuggestionInputControl();

            // Initially empty
            Assert.Empty(control.FilteredSuggestions);

            // Set suggestions
            var suggestions = new[] { "firefox", "chrome", "edge" };
            control.Suggestions = suggestions;
            Assert.Equal(suggestions, control.Suggestions);

            // Filter by text (case-insensitive)
            control.Text = "fire";
            var filtered = control.FilteredSuggestions.Cast<string>().ToList();
            Assert.Single(filtered);
            Assert.Contains("firefox", filtered);

            // Filter with uppercase
            control.Text = string.Empty;
            control.Suggestions = new[] { "Firefox", "Chrome", "Edge" };
            control.Text = "FIRE";
            filtered = control.FilteredSuggestions.Cast<string>().ToList();
            Assert.Single(filtered);
            Assert.Contains("Firefox", filtered);
        });
    }

    [Fact]
    public void Suggestions_Empty_List()
    {
        RunInSta(() =>
        {
            var control = new SuggestionInputControl();
            control.Suggestions = Array.Empty<string>();
            Assert.Empty(control.Suggestions);
        });
    }

    [Fact]
    public void Suggestions_Single_Item()
    {
        RunInSta(() =>
        {
            var control = new SuggestionInputControl();
            control.Suggestions = new[] { "firefox" };
            Assert.Single(control.Suggestions);
        });
    }

    [Fact]
    public void Suggestions_Multiple_Items()
    {
        RunInSta(() =>
        {
            var control = new SuggestionInputControl();
            var suggestions = new[] { "firefox", "chrome", "edge", "safari", "brave" };
            control.Suggestions = suggestions;
            Assert.Equal(suggestions, control.Suggestions);
        });
    }

    [Fact]
    public void FilteredSuggestions_No_Match()
    {
        RunInSta(() =>
        {
            var control = new SuggestionInputControl();
            control.Suggestions = new[] { "firefox", "chrome", "edge" };
            control.Text = "notepad";
            Assert.Empty(control.FilteredSuggestions);
        });
    }

    [Fact]
    public void FilteredSuggestions_EmptyText_WithoutManualOpen_IsEmpty()
    {
        RunInSta(() =>
        {
            var control = new SuggestionInputControl
            {
                Suggestions = new[] { "firefox", "chrome", "edge" },
                Text = string.Empty
            };

            // When user has not started typing and did not manually open the list,
            // filtered suggestions should stay empty.
            Assert.Empty(control.FilteredSuggestions);
        });
    }

    [Fact]
    public void FilteredSuggestions_WhitespaceText_WithoutManualOpen_IsEmpty()
    {
        RunInSta(() =>
        {
            var control = new SuggestionInputControl
            {
                Suggestions = new[] { "firefox", "chrome", "edge" },
                Text = "   "
            };

            Assert.Empty(control.FilteredSuggestions);
        });
    }

    [Fact]
    public void FilteredSuggestions_ManualOpen_ShowsAllSuggestions_ForEmptyText()
    {
        RunInSta(() =>
        {
            var control = new SuggestionInputControl
            {
                Suggestions = new[] { "firefox", "chrome", "edge" },
                Text = string.Empty
            };

            var combo = GetInputComboBox(control);
            var opened = typeof(SuggestionInputControl).GetMethod("InputComboBox_DropDownOpened", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.NotNull(opened);
            opened.Invoke(control, [combo, EventArgs.Empty]);

            var refresh = typeof(SuggestionInputControl).GetMethod("RefreshSuggestionFilter", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.NotNull(refresh);
            refresh.Invoke(control, []);

            var filtered = control.FilteredSuggestions.Cast<string>().ToList();
            Assert.Equal(3, filtered.Count);
            Assert.Contains("firefox", filtered);
            Assert.Contains("chrome", filtered);
            Assert.Contains("edge", filtered);
        });
    }

    [Fact]
    public void FilteredSuggestions_Ignores_NonString_And_WhitespaceItems()
    {
        RunInSta(() =>
        {
            var mixed = new ArrayList { "firefox", 123, null, "   ", "chrome" };
            var control = new SuggestionInputControl
            {
                Suggestions = mixed,
                Text = "c"
            };

            var filtered = control.FilteredSuggestions.Cast<string>().ToList();
            Assert.Single(filtered);
            Assert.Equal("chrome", filtered[0]);
        });
    }

    [Fact]
    public void FilteredSuggestions_Partial_Match()
    {
        RunInSta(() =>
        {
            var control = new SuggestionInputControl();
            control.Suggestions = new[] { "firefox", "firefoxESR", "chrome", "edge" };
            control.Text = "firefox";
            var filtered = control.FilteredSuggestions.Cast<string>().ToList();
            Assert.Equal(2, filtered.Count);
        });
    }

    [Fact]
    public void TextReplacement_Overwrites_Previous()
    {
        RunInSta(() =>
        {
            var control = new SuggestionInputControl();
            control.Text = "first";
            control.Text = "second";
            control.Text = "third";

            Assert.Equal("third", control.Text);
            Assert.NotEqual("first", control.Text);
            Assert.NotEqual("second", control.Text);
        });
    }

    [Fact]
    public void Suggestions_Null_Handling()
    {
        RunInSta(() =>
        {
            var control = new SuggestionInputControl();
            control.Suggestions = null;

            Assert.Null(control.Suggestions);
            Assert.Empty(control.FilteredSuggestions);
        });
    }

    [Fact]
    public void FilteredSuggestions_With_EmptySuggestions()
    {
        RunInSta(() =>
        {
            var control = new SuggestionInputControl();
            control.Suggestions = Array.Empty<string>();
            control.Text = "anything";

            Assert.Empty(control.FilteredSuggestions);
        });
    }

    [Fact]
    public void TextProperty_Null_Results_In_Null()
    {
        RunInSta(() =>
        {
            var control = new SuggestionInputControl { Text = "initial" };
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type
            control.Text = null!;
#pragma warning restore CS8625

            // Setting null results in null (not converted to empty)
            var result = control.Text;
            Assert.Null(result);
        });
    }

    [Fact]
    public void ComboBoxSelectionChanged_Empty_Selection()
    {
        RunInSta(() =>
        {
            var control = new SuggestionInputControl { Text = "existing" };
            var combo = new ComboBox { Text = "existing" };
            var args = new SelectionChangedEventArgs(
                Selector.SelectionChangedEvent,
                new ArrayList(),
                new ArrayList());

            InvokeSelectionChanged(control, combo, args);

            Assert.Equal("existing", control.Text);
        });
    }

    private static void InvokeSelectionChanged(SuggestionInputControl control, ComboBox combo, SelectionChangedEventArgs args)
    {
        var method = typeof(SuggestionInputControl).GetMethod("ComboBox_SelectionChanged", BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.NotNull(method);
        method.Invoke(control, [combo, args]);
    }

    [Fact]
    public void BackSpace_SingleCharacter_After_SelectionApplied()
    {
        RunInSta(() =>
        {
            var control = new SuggestionInputControl();
            var combo = GetInputComboBox(control);

            InvokeApplySelectedText(control, combo, "firefox");
            DrainDispatcher();

            Assert.Equal("firefox", control.Text);

            // Simulate one BackSpace edit after selection.
            combo.Text = "firefo";
            control.Text = combo.Text;

            Assert.Equal("firefo", control.Text);
        });
    }

    [Fact]
    public void BackSpace_KeyboardSelection_DropDownClosed_CommitsText()
    {
        RunInSta(() =>
        {
            var control = new SuggestionInputControl { Text = "fi" };
            var combo = GetInputComboBox(control);

            control.Suggestions = new[] { "firefox", "chrome" };
            combo.Text = "fi";
            SimulateOpenDropDownSelection(control, combo, () => combo.SelectedItem = "firefox");

            // Keyboard selection often commits on close; selected text should be reflected.
            InvokeDropDownClosed(control, combo);

            Assert.Equal("firefox", control.Text);
        });
    }

    [Fact]
    public void BackSpace_After_KeyboardSelection_CanClearToEmpty()
    {
        RunInSta(() =>
        {
            var control = new SuggestionInputControl { Text = "ed" };
            var combo = GetInputComboBox(control);

            control.Suggestions = new[] { "edge", "editor" };
            combo.Text = "ed";
            SimulateOpenDropDownSelection(control, combo, () => combo.SelectedItem = "edge");
            InvokeDropDownClosed(control, combo);

            Assert.Equal("edge", control.Text);

            // Simulate consecutive BackSpace until empty.
            control.Text = string.Empty;
            Assert.Equal(string.Empty, control.Text);
        });
    }

    [Fact]
    public void DropDownClosed_NoSelection_PreservesCurrentText()
    {
        RunInSta(() =>
        {
            var control = new SuggestionInputControl { Text = "calc" };
            var combo = GetInputComboBox(control);

            combo.Text = "calc";

            InvokeDropDownClosed(control, combo);

            Assert.Equal("calc", control.Text);
        });
    }

    [Fact]
    public void DropDownClosed_NoSelection_EmptyComboText_ClearsControlText()
    {
        RunInSta(() =>
        {
            var control = new SuggestionInputControl { Text = "calc" };
            var combo = GetInputComboBox(control);

            // User erased all text before closing dropdown.
            combo.Text = string.Empty;

            InvokeDropDownClosed(control, combo);

            Assert.Equal(string.Empty, control.Text);
            Assert.Equal(string.Empty, combo.Text);
        });
    }

    [Fact]
    public void DropDownClosed_WithSelection_UsesSelectedText()
    {
        RunInSta(() =>
        {
            var control = new SuggestionInputControl { Text = "fi" };
            var combo = GetInputComboBox(control);

            combo.ItemsSource = new[] { "firefox", "chrome" };
            combo.Text = "f";
            SimulateOpenDropDownSelection(control, combo, () => combo.SelectedItem = "firefox");

            InvokeDropDownClosed(control, combo);

            Assert.Equal("firefox", control.Text);
            Assert.Equal("firefox", combo.Text);
        });
    }

    [Fact]
    public void BackSpace_After_Selection_Then_TypeNewCharacter()
    {
        RunInSta(() =>
        {
            var control = new SuggestionInputControl();
            var combo = GetInputComboBox(control);

            InvokeApplySelectedText(control, combo, "chrome");
            DrainDispatcher();
            Assert.Equal("chrome", control.Text);

            // BackSpace once and type a new character.
            control.Text = "chrom";
            control.Text += "s";

            Assert.Equal("chroms", control.Text);
        });
    }

    [Fact]
    public void InputComboBoxStyle_CanSetAndGet()
    {
        RunInSta(() =>
        {
            var control = new SuggestionInputControl();
            var style = new Style(typeof(ComboBox));

            control.InputComboBoxStyle = style;

            Assert.Same(style, control.InputComboBoxStyle);
        });
    }

    private static ComboBox GetInputComboBox(SuggestionInputControl control)
    {
        var combo = control.FindName("InputComboBox") as ComboBox;
        Assert.NotNull(combo);
        return combo;
    }

    private static void InvokeDropDownClosed(SuggestionInputControl control, ComboBox combo)
    {
        var method = typeof(SuggestionInputControl).GetMethod("InputComboBox_DropDownClosed", BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.NotNull(method);
        method.Invoke(control, [combo, EventArgs.Empty]);
    }

    private static void InvokeApplySelectedText(SuggestionInputControl control, ComboBox combo, string selectedText)
    {
        var method = typeof(SuggestionInputControl).GetMethod("ApplySelectedText", BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.NotNull(method);
        method.Invoke(control, [combo, selectedText]);
    }

    private static void DrainDispatcher()
    {
        Dispatcher.CurrentDispatcher.Invoke(() => { }, DispatcherPriority.ApplicationIdle);
    }

    /// <summary>
    /// Simulates arrow-key navigation while the dropdown is open.
    /// In the real app, <c>ComboBox_SelectionChanged</c> returns early because
    /// <c>IsDropDownOpen</c> is <c>true</c>. In unit tests the property is
    /// coerced to <c>false</c> (no visual tree), so we set
    /// <c>_suppressing</c> via reflection to replicate
    /// the "dropdown open" behaviour.
    /// </summary>
    private static void SimulateOpenDropDownSelection(SuggestionInputControl control, ComboBox combo, Action action)
    {
        var field = typeof(SuggestionInputControl).GetField(
            "_suppressing", BindingFlags.Instance | BindingFlags.NonPublic)!;
        field.SetValue(control, true);
        try
        {
            action();
        }
        finally
        {
            field.SetValue(control, false);
        }
    }

    [Fact]
    public void ComplexFlow_TypeSelectDeleteRetype_MaintainsConsistency()
    {
        RunInSta(() =>
        {
            var control = new SuggestionInputControl();
            var combo = GetInputComboBox(control);
            control.Suggestions = new[] { "firefox", "firefoxESR", "chrome", "edge" };

            // 1. Type partial → filter narrows
            control.Text = "fire";
            var filtered = control.FilteredSuggestions.Cast<string>().ToList();
            Assert.Equal(2, filtered.Count);
            Assert.Contains("firefox", filtered);
            Assert.Contains("firefoxESR", filtered);

            // 2. Select candidate via ComboBox selection
            InvokeApplySelectedText(control, combo, "firefox");
            DrainDispatcher();
            Assert.Equal("firefox", control.Text);

            // 3. Delete (backspace simulation) to partial
            control.Text = "fire";
            filtered = control.FilteredSuggestions.Cast<string>().ToList();
            Assert.Equal(2, filtered.Count);

            // 4. Delete further to empty
            control.Text = string.Empty;
            Assert.Empty(control.FilteredSuggestions);

            // 5. Retype completely different input
            control.Text = "chr";
            filtered = control.FilteredSuggestions.Cast<string>().ToList();
            Assert.Single(filtered);
            Assert.Equal("chrome", filtered[0]);

            // 6. Select the new candidate
            InvokeApplySelectedText(control, combo, "chrome");
            DrainDispatcher();
            Assert.Equal("chrome", control.Text);
        });
    }

    [Fact]
    public void ComplexFlow_SelectThenChangeToAnotherCandidate()
    {
        RunInSta(() =>
        {
            var control = new SuggestionInputControl();
            var combo = GetInputComboBox(control);
            control.Suggestions = new[] { "notepad", "notepad++", "nano" };

            // Select first candidate
            InvokeApplySelectedText(control, combo, "notepad");
            DrainDispatcher();
            Assert.Equal("notepad", control.Text);

            // Overwrite with different text entirely
            control.Text = "nano";
            var filtered = control.FilteredSuggestions.Cast<string>().ToList();
            Assert.Single(filtered);
            Assert.Equal("nano", filtered[0]);

            // Select the new candidate
            InvokeApplySelectedText(control, combo, "nano");
            DrainDispatcher();
            Assert.Equal("nano", control.Text);
        });
    }

    [Fact]
    public void ComplexFlow_SuggestionsSwappedMidInput_FiltersUpdated()
    {
        RunInSta(() =>
        {
            var control = new SuggestionInputControl();
            control.Suggestions = new[] { "alpha", "beta", "gamma" };
            control.Text = "al";

            var filtered = control.FilteredSuggestions.Cast<string>().ToList();
            Assert.Single(filtered);
            Assert.Equal("alpha", filtered[0]);

            // Swap entire suggestions list while text still set
            control.Suggestions = new[] { "alpine", "algebra", "delta" };

            filtered = control.FilteredSuggestions.Cast<string>().ToList();
            Assert.Equal(2, filtered.Count);
            Assert.Contains("alpine", filtered);
            Assert.Contains("algebra", filtered);
        });
    }

    [Fact]
    public void ComplexFlow_KeyboardSelectThenBackspaceToEmpty()
    {
        RunInSta(() =>
        {
            var control = new SuggestionInputControl { Text = "ed" };
            var combo = GetInputComboBox(control);
            combo.ItemsSource = new[] { "edge", "editor", "eclipse" };
            combo.Text = "ed";

            // Keyboard (arrow key) selection
            SimulateOpenDropDownSelection(control, combo, () => combo.SelectedItem = "editor");
            InvokeDropDownClosed(control, combo);
            Assert.Equal("editor", control.Text);

            // Backspace step by step
            control.Text = "edito";
            control.Text = "edit";
            control.Text = "edi";
            control.Text = "ed";
            control.Text = "e";
            control.Text = string.Empty;

            Assert.Equal(string.Empty, control.Text);
            Assert.Empty(control.FilteredSuggestions);
        });
    }

    [Fact]
    public void ComplexFlow_SelectClearSelectDifferent_AllTransitionsWork()
    {
        RunInSta(() =>
        {
            var control = new SuggestionInputControl();
            var combo = GetInputComboBox(control);
            control.Suggestions = new[] { "git", "github", "gitlab", "go" };

            // First selection
            control.Text = "git";
            InvokeApplySelectedText(control, combo, "github");
            DrainDispatcher();
            Assert.Equal("github", control.Text);

            // Clear everything
            control.Text = string.Empty;
            Assert.Empty(control.FilteredSuggestions);

            // Second selection (different candidate)
            control.Text = "go";
            var filtered = control.FilteredSuggestions.Cast<string>().ToList();
            Assert.Single(filtered);
            Assert.Equal("go", filtered[0]);

            InvokeApplySelectedText(control, combo, "go");
            DrainDispatcher();
            Assert.Equal("go", control.Text);

            // Third selection after partial delete + retype
            control.Text = "g";
            filtered = control.FilteredSuggestions.Cast<string>().ToList();
            Assert.Equal(4, filtered.Count);

            InvokeApplySelectedText(control, combo, "gitlab");
            DrainDispatcher();
            Assert.Equal("gitlab", control.Text);
        });
    }

    [Fact]
    public void ComplexFlow_RapidTextChanges_FilterStaysConsistent()
    {
        RunInSta(() =>
        {
            var control = new SuggestionInputControl();
            control.Suggestions = new[] { "python", "pypy", "perl", "php", "powershell" };

            // Rapid sequence simulating fast typing
            control.Text = "p";
            Assert.Equal(5, control.FilteredSuggestions.Cast<string>().Count());

            control.Text = "py";
            Assert.Equal(2, control.FilteredSuggestions.Cast<string>().Count());

            control.Text = "pyt";
            Assert.Single(control.FilteredSuggestions.Cast<string>());
            Assert.Equal("python", control.FilteredSuggestions.Cast<string>().First());

            // Backspace rapidly
            control.Text = "py";
            Assert.Equal(2, control.FilteredSuggestions.Cast<string>().Count());

            control.Text = "p";
            Assert.Equal(5, control.FilteredSuggestions.Cast<string>().Count());

            // Switch direction to different prefix
            control.Text = "po";
            Assert.Single(control.FilteredSuggestions.Cast<string>());
            Assert.Equal("powershell", control.FilteredSuggestions.Cast<string>().First());
        });
    }

    [Fact]
    public void ComplexFlow_DropDownClosedThenImmediateEdit_TextPreserved()
    {
        RunInSta(() =>
        {
            var control = new SuggestionInputControl { Text = "vi" };
            var combo = GetInputComboBox(control);
            combo.ItemsSource = new[] { "vim", "visual studio", "vscode" };

            // Keyboard select + close
            combo.Text = "vi";
            SimulateOpenDropDownSelection(control, combo, () => combo.SelectedItem = "vim");
            InvokeDropDownClosed(control, combo);
            Assert.Equal("vim", control.Text);

            // Immediately continue editing (append character)
            control.Text = "vimrc";
            Assert.Equal("vimrc", control.Text);

            // No suggestion matches "vimrc"
            control.Suggestions = new[] { "vim", "visual studio", "vscode" };
            Assert.Empty(control.FilteredSuggestions);
        });
    }

    [Fact]
    public void ComplexFlow_SuggestionsNulledMidInput_FilteredBecomesEmpty()
    {
        RunInSta(() =>
        {
            var control = new SuggestionInputControl();
            control.Suggestions = new[] { "docker", "dotnet" };
            control.Text = "do";
            Assert.Equal(2, control.FilteredSuggestions.Cast<string>().Count());

            // Suggestions source removed while user is typing
            control.Suggestions = null;
            Assert.Empty(control.FilteredSuggestions);

            // Text remains intact
            Assert.Equal("do", control.Text);
        });
    }

    [Fact]
    public void SuggestionsChanged_ResetsSelectedIndex_SoDownKeyStartsFromTop()
    {
        RunInSta(() =>
        {
            var control = new SuggestionInputControl();
            var combo = GetInputComboBox(control);
            control.Suggestions = new[] { "alpha", "beta", "gamma" };
            control.Text = "a";

            // Simulate user navigating to an item with arrow keys
            SimulateOpenDropDownSelection(control, combo, () => combo.SelectedIndex = 0);
            Assert.Equal(0, combo.SelectedIndex);

            // Now suggestions change (e.g. user types more or ViewModel refreshes)
            control.Suggestions = new[] { "ant", "apple", "avocado" };

            // SelectedIndex should be reset so the next down-key starts from the top
            Assert.Equal(-1, combo.SelectedIndex);
        });
    }

    [Fact]
    public void TextChanged_ResetsSelectedIndex_ViaFilterRefresh()
    {
        RunInSta(() =>
        {
            var control = new SuggestionInputControl();
            var combo = GetInputComboBox(control);
            control.Suggestions = new[] { "firefox", "firefoxESR", "chrome" };
            control.Text = "fire";

            // Simulate arrow-key navigation
            SimulateOpenDropDownSelection(control, combo, () => combo.SelectedIndex = 1);

            // Text change triggers filter refresh which clears SelectedItem,
            // resetting SelectedIndex to -1.
            control.Text = "firef";

            Assert.Equal(-1, combo.SelectedIndex);
        });
    }

    /// <summary>
    /// Regression: open category ComboBox → arrow-down to select → BackSpace
    /// should delete text, not be silently swallowed.
    /// </summary>
    [Fact]
    public void DropDownClosed_KeyboardSelect_ThenBackSpace_TextIsEditable()
    {
        RunInStaWithDispatcher(() =>
        {
            var control = new SuggestionInputControl();
            var combo = GetInputComboBox(control);
            control.Suggestions = new[] { "Gaming", "Development", "Utility" };
            DrainDispatcher();

            // Step 1: open dropdown manually (click on toggle button).
            InvokeDropDownOpened(control, combo);
            DrainDispatcher();

            // Step 2: arrow-down selects "Gaming" while dropdown is open.
            SimulateOpenDropDownSelection(control, combo, () =>
            {
                combo.SelectedItem = "Gaming";
            });
            Assert.Equal("Gaming", combo.SelectedItem as string);

            // Step 3: dropdown closes (Enter key or click-away).
            InvokeDropDownClosed(control, combo);
            DrainDispatcher();

            Assert.Equal("Gaming", control.Text);

            // Step 4: user presses BackSpace — text must actually change.
            control.Text = "Gamin";
            DrainDispatcher();
            Assert.Equal("Gamin", control.Text);

            // Continue deleting to empty.
            control.Text = string.Empty;
            DrainDispatcher();
            Assert.Equal(string.Empty, control.Text);
        });
    }

    /// <summary>
    /// Verifies that after DropDownClosed with keyboard selection, text
    /// is preserved when Suggestions changes trigger a filter refresh.
    /// </summary>
    [Fact]
    public void DropDownClosed_KeyboardSelect_TextPreserved_AfterFilterRefresh()
    {
        RunInStaWithDispatcher(() =>
        {
            var control = new SuggestionInputControl();
            var combo = GetInputComboBox(control);
            control.Suggestions = new[] { "alpha", "beta" };
            DrainDispatcher();

            // Simulate manual open
            InvokeDropDownOpened(control, combo);
            DrainDispatcher();

            // Arrow-down to "alpha"
            SimulateOpenDropDownSelection(control, combo, () => combo.SelectedItem = "alpha");

            // Dropdown closes
            InvokeDropDownClosed(control, combo);
            DrainDispatcher();

            Assert.Equal("alpha", control.Text);

            // Now trigger a FilteredSuggestions refresh.
            control.Suggestions = new[] { "alpha", "beta", "gamma" };
            DrainDispatcher();

            Assert.Equal("alpha", control.Text);
        });
    }

    /// <summary>
    /// Regression: The exact user scenario that reproduces the bug.
    /// Open category ComboBox → press Down to "Gaming" → press Enter (close) → press BackSpace.
    /// Expected: text changes from "Gaming" to "Gamin".
    /// Bug: text stays "Gaming" — BackSpace is swallowed.
    ///
    /// In a real WPF app with a visual tree, after ApplySelectedText finishes and
    /// lifts the suppression guard, WPF's ComboBox detects that
    /// Text = "Gaming" matches an ItemsSource item and fires SelectionChanged.
    /// This re-enters ComboBox_SelectionChanged → ApplySelectedText, potentially
    /// locking the text. We simulate this WPF auto-selection explicitly because
    /// it does not occur in the test environment (no visual tree).
    /// </summary>
    [Fact]
    public void UserScenario_OpenDropDown_ArrowDown_Close_BackSpace_TextChanges()
    {
        RunInStaWithDispatcher(() =>
        {
            // --- Arrange: control with category suggestions ---
            var control = new SuggestionInputControl();
            var combo = GetInputComboBox(control);
            control.Suggestions = new[] { "Gaming", "Development", "Utility" };
            DrainDispatcher();

            // --- Act 1: User clicks the dropdown button → dropdown opens ---
            InvokeDropDownOpened(control, combo);
            DrainDispatcher();

            // --- Act 2: User presses Down arrow → "Gaming" is highlighted ---
            SimulateOpenDropDownSelection(control, combo, () =>
            {
                combo.SelectedItem = "Gaming";
            });
            Assert.Equal("Gaming", combo.SelectedItem as string);

            // --- Act 3: User presses Enter → dropdown closes ---
            InvokeDropDownClosed(control, combo);
            DrainDispatcher();

            SimulateWpfAutoSelection(control, combo, "Gaming");
            DrainDispatcher();

            Assert.Equal("Gaming", control.Text);
            Assert.Equal("Gaming", combo.Text);

            // --- Act 4: User presses BackSpace → text should change ---
            control.Text = "Gamin";
            DrainDispatcher();

            SimulateWpfAutoSelection(control, combo, "Gaming");
            DrainDispatcher();

            // CRITICAL: Text must be "Gamin" after BackSpace, not reverted to "Gaming".
            Assert.Equal("Gamin", control.Text);
            Assert.Equal("Gamin", combo.Text);
        });
    }

    /// <summary>
    /// Verifies that WPF auto-selection after DropDownClosed doesn't permanently
    /// lock the text. After each text change, WPF may fire SelectionChanged when
    /// the text matches or prefixes an ItemsSource item. Our code must handle this
    /// without reverting the user's edits.
    /// </summary>
    [Fact]
    public void UserScenario_OpenDropDown_ArrowDown_Close_VerifyFilteredSuggestions()
    {
        RunInStaWithDispatcher(() =>
        {
            var control = new SuggestionInputControl();
            var combo = GetInputComboBox(control);
            control.Suggestions = new[] { "Gaming", "Development", "Utility" };
            DrainDispatcher();

            // Open → arrow-down → close
            InvokeDropDownOpened(control, combo);
            DrainDispatcher();
            SimulateOpenDropDownSelection(control, combo, () => combo.SelectedItem = "Gaming");
            InvokeDropDownClosed(control, combo);
            DrainDispatcher();

            // WPF auto-selects after close
            SimulateWpfAutoSelection(control, combo, "Gaming");
            DrainDispatcher();

            Assert.Equal("Gaming", control.Text);

            // BackSpace: "Gaming" → "Gamin"
            control.Text = "Gamin";
            DrainDispatcher();

            // WPF auto-selection after text change
            SimulateWpfAutoSelection(control, combo, "Gaming");
            DrainDispatcher();

            // FilteredSuggestions should contain "Gaming" (matches "Gamin")
            var filtered = control.FilteredSuggestions.Cast<string>().ToList();
            Assert.Contains("Gaming", filtered);
            Assert.DoesNotContain("Development", filtered);
            Assert.DoesNotContain("Utility", filtered);

            // Text must remain "Gamin"
            Assert.Equal("Gamin", control.Text);
        });
    }

    /// <summary>
    /// Tests the scenario where user opens dropdown, arrow-selects, closes,
    /// then immediately types a completely different text.
    /// WPF auto-selection fires after each transition.
    /// </summary>
    [Fact]
    public void UserScenario_OpenDropDown_ArrowDown_Close_TypeNewChar_TextChanges()
    {
        RunInStaWithDispatcher(() =>
        {
            var control = new SuggestionInputControl();
            var combo = GetInputComboBox(control);
            control.Suggestions = new[] { "Gaming", "Development", "Utility" };
            DrainDispatcher();

            // Open → arrow-down → close
            InvokeDropDownOpened(control, combo);
            DrainDispatcher();
            SimulateOpenDropDownSelection(control, combo, () => combo.SelectedItem = "Gaming");
            InvokeDropDownClosed(control, combo);
            DrainDispatcher();

            // WPF auto-selection after close
            SimulateWpfAutoSelection(control, combo, "Gaming");
            DrainDispatcher();

            Assert.Equal("Gaming", control.Text);

            // User replaces text entirely with "Dev"
            control.Text = "Dev";
            DrainDispatcher();

            Assert.Equal("Dev", control.Text);
            Assert.Equal("Dev", combo.Text);

            var filtered = control.FilteredSuggestions.Cast<string>().ToList();
            Assert.Contains("Development", filtered);
            Assert.DoesNotContain("Gaming", filtered);
        });
    }

    /// <summary>
    /// Tests repeated open-select-close cycles with WPF auto-selection simulation.
    /// Ensures state doesn't accumulate across cycles.
    /// </summary>
    [Fact]
    public void UserScenario_RepeatedOpenSelectClose_BackSpaceStillWorks()
    {
        RunInStaWithDispatcher(() =>
        {
            var control = new SuggestionInputControl();
            var combo = GetInputComboBox(control);
            control.Suggestions = new[] { "Gaming", "Development", "Utility" };
            DrainDispatcher();

            // First cycle: select "Gaming"
            InvokeDropDownOpened(control, combo);
            DrainDispatcher();
            SimulateOpenDropDownSelection(control, combo, () => combo.SelectedItem = "Gaming");
            InvokeDropDownClosed(control, combo);
            DrainDispatcher();
            SimulateWpfAutoSelection(control, combo, "Gaming");
            DrainDispatcher();

            Assert.Equal("Gaming", control.Text);

            // BackSpace
            control.Text = "Gamin";
            DrainDispatcher();
            SimulateWpfAutoSelection(control, combo, "Gaming");
            DrainDispatcher();
            Assert.Equal("Gamin", control.Text);

            // Clear text
            control.Text = string.Empty;
            DrainDispatcher();

            // Second cycle: select "Development"
            InvokeDropDownOpened(control, combo);
            DrainDispatcher();
            SimulateOpenDropDownSelection(control, combo, () => combo.SelectedItem = "Development");
            InvokeDropDownClosed(control, combo);
            DrainDispatcher();
            SimulateWpfAutoSelection(control, combo, "Development");
            DrainDispatcher();

            Assert.Equal("Development", control.Text);

            // BackSpace on second selection
            control.Text = "Developmen";
            DrainDispatcher();
            SimulateWpfAutoSelection(control, combo, "Development");
            DrainDispatcher();
            Assert.Equal("Developmen", control.Text);
            Assert.Equal("Developmen", combo.Text);
        });
    }

    /// <summary>
    /// Simulates WPF's internal auto-selection behavior.
    /// In a real WPF app with a visual tree, when ItemsSource is replaced or Text
    /// matches an ItemsSource item, WPF fires SelectionChanged. This doesn't happen
    /// in the test environment (no visual tree), so we fire it explicitly.
    /// </summary>
    private static void SimulateWpfAutoSelection(SuggestionInputControl control, ComboBox combo, string itemText)
    {
        var args = new SelectionChangedEventArgs(
            Selector.SelectionChangedEvent,
            new ArrayList(),
            new ArrayList { itemText });
        InvokeSelectionChanged(control, combo, args);
    }

    /// <summary>
    /// Simulates the side-effect of <c>EditableTextBox_TextChanged</c> calling
    /// <c>RegisterUserInput()</c>, which clears <c>_showAllSuggestions</c>
    /// and sets <c>_userEditedWhileOpen</c> when the dropdown is open.
    /// In tests <c>IsDropDownOpen</c> is coerced to <c>false</c> (no visual tree),
    /// so we set the flag directly via reflection instead of calling
    /// <c>RegisterUserInput</c>, which checks <c>IsDropDownOpen</c>.
    /// </summary>
    private static void SimulateRegisterUserInput(SuggestionInputControl control)
    {
        var method = typeof(SuggestionInputControl).GetMethod(
            "RegisterUserInput", BindingFlags.Instance | BindingFlags.NonPublic)!;
        method.Invoke(control, null);

        // In real WPF, IsDropDownOpen is true here so RegisterUserInput sets this.
        // Tests can't open the dropdown, so set it directly.
        var field = typeof(SuggestionInputControl).GetField(
            "_userEditedWhileOpen", BindingFlags.Instance | BindingFlags.NonPublic)!;
        field.SetValue(control, true);
    }

    /// <summary>
    /// Reproduces the exact bug scenario:
    ///   Open dropdown → Down arrow → BackSpace → text should be empty.
    ///
    /// In the real WPF app:
    /// 1. User clicks the dropdown button → dropdown opens,
    ///    <c>_showAllSuggestions = true</c>, all items shown.
    /// 2. User presses Down → WPF sets <c>SelectedItem = "Gaming"</c>,
    ///    <c>PART_EditableTextBox</c> shows "Gaming" (all selected).
    ///    <c>ComboBox_SelectionChanged</c> returns early (<c>IsDropDownOpen</c>).
    /// 3. User presses BackSpace → all selected text deleted → text = "".
    ///    <c>EditableTextBox_TextChanged</c> → <c>RegisterUserInput</c>
    ///    (clears <c>_showAllSuggestions</c>)
    ///    → <c>SyncTextFromEditableTextBox</c> → <c>Text = ""</c>
    ///    → <c>RefreshSuggestionFilter</c> (empty keyword, flag false → empty list)
    ///    → <c>UpdateDropDownVisibility</c> (no suggestions → closes dropdown)
    ///    → <c>DropDownClosed</c> fires.
    /// 4. <c>DropDownClosed</c> sees <c>SelectedItem = "Gaming"</c> (still set
    ///    from step 2) → calls <c>ApplySelectedText("Gaming")</c> → text reverts!
    ///
    /// Bug: text becomes "Gaming" again instead of staying "".
    /// </summary>
    [Fact]
    public void Bug_OpenDropDown_ArrowDown_BackSpaceAll_TextReverts()
    {
        RunInStaWithDispatcher(() =>
        {
            var control = new SuggestionInputControl();
            var combo = GetInputComboBox(control);
            control.Suggestions = new[] { "Gaming", "Development", "Utility" };
            DrainDispatcher();

            // Step 1: User clicks dropdown button → dropdown opens
            InvokeDropDownOpened(control, combo);
            DrainDispatcher();

            // Step 2: User presses Down → SelectedItem = "Gaming"
            // ComboBox_SelectionChanged returns early since IsDropDownOpen is true.
            SimulateOpenDropDownSelection(control, combo, () => combo.SelectedItem = "Gaming");
            // WPF internally syncs the editable TextBox to show selected item's text.
            control.Text = "Gaming";
            DrainDispatcher();

            Assert.Equal("Gaming", combo.SelectedItem as string);
            Assert.Equal("Gaming", control.Text);

            // Step 3: User presses BackSpace
            SimulateRegisterUserInput(control);
            control.Text = "";
            DrainDispatcher();

            // Step 4: In real WPF, empty FilteredSuggestions → dropdown closes.
            // DropDownClosed sees SelectedItem="Gaming" → bug occurs here.
            InvokeDropDownClosed(control, combo);
            DrainDispatcher();

            // Text should be "" because user deleted it with BackSpace.
            Assert.Equal("", control.Text);
            Assert.Equal("", combo.Text);
        });
    }

    /// <summary>
    /// Variant: user moves cursor to end of text after Down arrow, then
    /// presses BackSpace once to delete one character ("Gaming" → "Gamin").
    /// Dropdown stays open (FilteredSuggestions still contains "Gaming").
    /// Then user clicks away → dropdown closes → text reverts.
    /// </summary>
    [Fact]
    public void Bug_OpenDropDown_ArrowDown_BackSpacePartial_ThenClose_TextReverts()
    {
        RunInStaWithDispatcher(() =>
        {
            var control = new SuggestionInputControl();
            var combo = GetInputComboBox(control);
            control.Suggestions = new[] { "Gaming", "Development", "Utility" };
            DrainDispatcher();

            // Step 1: Open dropdown
            InvokeDropDownOpened(control, combo);
            DrainDispatcher();

            // Step 2: Down → "Gaming"
            SimulateOpenDropDownSelection(control, combo, () => combo.SelectedItem = "Gaming");
            control.Text = "Gaming";
            DrainDispatcher();

            // Step 3: User presses End (deselect text), then BackSpace once → "Gamin"
            SimulateRegisterUserInput(control);
            control.Text = "Gamin";
            DrainDispatcher();

            // Dropdown stays open (FilteredSuggestions has "Gaming" matching "Gamin")
            var filtered = control.FilteredSuggestions.Cast<string>().ToList();
            Assert.Contains("Gaming", filtered);

            // Step 4: User clicks away → dropdown closes
            InvokeDropDownClosed(control, combo);
            DrainDispatcher();

            // Text should be "Gamin" — user's edit must not be reverted.
            Assert.Equal("Gamin", control.Text);
            Assert.Equal("Gamin", combo.Text);
        });
    }

    /// <summary>
    /// Variant: user opens dropdown, presses Down to select "Gaming",
    /// then types "D" to replace the selected text → text = "D".
    /// Dropdown may close and reopen based on filter results.
    /// Text must not revert to "Gaming".
    /// </summary>
    [Fact]
    public void Bug_OpenDropDown_ArrowDown_TypeOverSelection_TextReplacedNotReverted()
    {
        RunInStaWithDispatcher(() =>
        {
            var control = new SuggestionInputControl();
            var combo = GetInputComboBox(control);
            control.Suggestions = new[] { "Gaming", "Development", "Utility" };
            DrainDispatcher();

            // Open → Down → "Gaming"
            InvokeDropDownOpened(control, combo);
            DrainDispatcher();
            SimulateOpenDropDownSelection(control, combo, () => combo.SelectedItem = "Gaming");
            control.Text = "Gaming";
            DrainDispatcher();

            // User types "D" (all text was selected, so it's replaced)
            SimulateRegisterUserInput(control);
            control.Text = "D";
            DrainDispatcher();

            // Dropdown closes or stays open depending on filter.
            // Simulate close to test DropDownClosed doesn't revert:
            InvokeDropDownClosed(control, combo);
            DrainDispatcher();

            Assert.Equal("D", control.Text);
            Assert.Equal("D", combo.Text);
        });
    }

    private static void InvokeDropDownOpened(SuggestionInputControl control, ComboBox combo)
    {
        var method = typeof(SuggestionInputControl).GetMethod(
            "InputComboBox_DropDownOpened", BindingFlags.Instance | BindingFlags.NonPublic)!;
        method.Invoke(control, [combo, EventArgs.Empty]);
    }

    /// <summary>
    /// Runs <paramref name="action"/> on an STA thread with a running Dispatcher,
    /// so deferred operations (BeginInvoke at Render/Input priority) execute during
    /// <see cref="DrainDispatcher"/> calls. This closely mimics real WPF runtime behavior.
    /// </summary>
    private static void RunInStaWithDispatcher(Action action)
    {
        Exception? captured = null;

        var thread = new Thread(() =>
        {
            // Push a frame that executes the action then shuts down.
            Dispatcher.CurrentDispatcher.BeginInvoke(() =>
            {
                try
                {
                    action();
                }
                catch (Exception ex)
                {
                    captured = ex;
                }
                finally
                {
                    Dispatcher.CurrentDispatcher.BeginInvokeShutdown(DispatcherPriority.ApplicationIdle);
                }
            });

            Dispatcher.Run();
        });

        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        thread.Join();

        if (captured is not null)
        {
            ExceptionDispatchInfo.Capture(captured).Throw();
        }
    }

    private static void RunInSta(Action action)
    {
        Exception? captured = null;

        var thread = new Thread(() =>
        {
            try
            {
                action();
            }
            catch (Exception ex)
            {
                captured = ex;
            }
        });

        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        thread.Join();

        if (captured is not null)
        {
            ExceptionDispatchInfo.Capture(captured).Throw();
        }
    }
}

