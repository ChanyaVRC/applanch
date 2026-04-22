using System.Reflection;
using System.Runtime.Versioning;
using System.Windows;
using applanch.Tests.ThemeCreator.TestSupport;
using applanch.ThemeCreator;
using Xunit;

namespace applanch.Tests.ThemeCreator;

[Collection("WpfTests")]
public sealed class ThemeCreatorWindowTests
{
    [Fact]
    public void ResolvePreviewBaseThemeId_PrefersExplicitBase_ThenCurrentTheme_ThenLight()
    {
        Assert.Equal("dark", ThemeCreatorWindow.ResolvePreviewBaseThemeId("dark", "light"));
        Assert.Equal("dark", ThemeCreatorWindow.ResolvePreviewBaseThemeId(null, "dark"));
        Assert.Equal("light", ThemeCreatorWindow.ResolvePreviewBaseThemeId(null, "system"));
        Assert.Equal("light", ThemeCreatorWindow.ResolvePreviewBaseThemeId(null, null));
    }

    [Fact]
    public void ResolvePreviewBaseThemeId_WithAvailableThemeIds_FallsBackToLightForUnknownCurrentTheme()
    {
        var available = new[] { "light", "dark" };

        var resolved = ThemeCreatorWindow.ResolvePreviewBaseThemeId(null, "themecreator-preview", available);

        Assert.Equal("light", resolved);
    }

    [Fact]
    public void ResolvePreviewBaseThemeId_WithAvailableThemeIds_FallsBackToLightForUnknownSelectedTheme()
    {
        var available = new[] { "light", "dark" };

        var resolved = ThemeCreatorWindow.ResolvePreviewBaseThemeId("custom-theme", "dark", available);

        Assert.Equal("light", resolved);
    }

    [Fact]
    public void ResolveAppPreviewThemeDirectory_UsesApplanchRuntimeDirectory()
    {
        var workspaceRoot = Path.Combine("C:", "repo", "applanch-pr-theme-palette");
        var targetFramework = typeof(ThemeCreatorWindow).Assembly
            .GetCustomAttribute<TargetFrameworkAttribute>()
            ?.FrameworkName;
        Assert.False(string.IsNullOrWhiteSpace(targetFramework));

        var resolved = ThemeCreatorWindow.ResolveAppPreviewThemeDirectory(workspaceRoot);

#if DEBUG
        var expected = Path.Combine(
            workspaceRoot,
            "src",
            "applanch",
            "bin",
            "Debug",
            targetFramework!,
            "Config",
            "UserDefined",
            "theme-palette");
        Assert.Equal(expected, resolved);
#else
        var expected = Path.Combine(
            AppContext.BaseDirectory,
            "Config",
            "UserDefined",
            "theme-palette");
        Assert.Equal(expected, resolved);
#endif
    }

    [Fact]
    public void Window_LoadsEntriesAndSuggestsUserDefinedOutputPath()
    {
        WpfTestHost.RunInSta(() =>
        {
            WpfTestHost.EnsureApplication();

            var window = new ThemeCreatorWindow();
            window.SourcePathTextBox.Text = ResolveBundledThemePalettePath();
            InvokeReloadThemes(window);
            WpfTestHost.ShowOffscreen(window);

            window.ThemeIdTextBox.Text = "user-editable-theme";
            WpfTestHost.DoEvents();

            Assert.EndsWith(
                Path.Combine("Config", "UserDefined", "theme-palette", "user-editable-theme.json"),
                window.OutputPathTextBox.Text,
                StringComparison.OrdinalIgnoreCase);

            var entries = window.EntriesDataGrid.Items.Cast<ThemeCreatorEditableEntry>().ToArray();
            Assert.NotEmpty(entries);
            Assert.Contains(entries, static entry =>
                string.Equals(entry.Key, "Brush.AppBackground", StringComparison.Ordinal) &&
                !string.IsNullOrWhiteSpace(entry.Description));

            window.Close();
        });
    }

    [Fact]
    public void Window_StartsWithNoBaseThemeSelected_AndEditableEntriesVisible()
    {
        WpfTestHost.RunInSta(() =>
        {
            WpfTestHost.EnsureApplication();

            var window = new ThemeCreatorWindow();
            window.SourcePathTextBox.Text = ResolveBundledThemePalettePath();
            InvokeReloadThemes(window);
            WpfTestHost.ShowOffscreen(window);

            var selected = Assert.IsType<ThemeCreatorBaseThemeChoice>(window.BaseThemeComboBox.SelectedItem);
            Assert.Null(selected.Id);
            var entries = window.EntriesDataGrid.Items.Cast<ThemeCreatorEditableEntry>().ToArray();
            Assert.NotEmpty(entries);
            Assert.All(entries, static entry => Assert.False(string.IsNullOrWhiteSpace(entry.Hex)));
            Assert.True(window.EntriesDataGrid.IsEnabled);

            window.Close();
        });
    }

    [Fact]
    public void SwitchingBaseTheme_NoneToLightToNone_KeepsEntriesEditable()
    {
        WpfTestHost.RunInSta(() =>
        {
            WpfTestHost.EnsureApplication();

            var window = new ThemeCreatorWindow();
            window.SourcePathTextBox.Text = ResolveBundledThemePalettePath();
            InvokeReloadThemes(window);
            WpfTestHost.ShowOffscreen(window);

            window.BaseThemeComboBox.SelectedItem = window.BaseThemeComboBox.Items
                .Cast<ThemeCreatorBaseThemeChoice>()
                .First(static choice => string.Equals(choice.Id, "light", StringComparison.OrdinalIgnoreCase));
            WpfTestHost.DoEvents();

            Assert.True(window.EntriesDataGrid.IsEnabled);
            Assert.NotEmpty(window.EntriesDataGrid.Items.Cast<ThemeCreatorEditableEntry>());

            window.BaseThemeComboBox.SelectedItem = window.BaseThemeComboBox.Items
                .Cast<ThemeCreatorBaseThemeChoice>()
                .First(static choice => choice.Id is null);
            WpfTestHost.DoEvents();

            Assert.True(window.EntriesDataGrid.IsEnabled);
            Assert.NotEmpty(window.EntriesDataGrid.Items.Cast<ThemeCreatorEditableEntry>());

            window.BaseThemeComboBox.SelectedItem = window.BaseThemeComboBox.Items
                .Cast<ThemeCreatorBaseThemeChoice>()
                .First(static choice => string.Equals(choice.Id, "light", StringComparison.OrdinalIgnoreCase));
            WpfTestHost.DoEvents();

            Assert.True(window.EntriesDataGrid.IsEnabled);
            Assert.NotEmpty(window.EntriesDataGrid.Items.Cast<ThemeCreatorEditableEntry>());

            window.Close();
        });
    }

    [Fact]
    public void Window_ThemeIdAndOutputPath_EnableCreateButton()
    {
        WpfTestHost.RunInSta(() =>
        {
            WpfTestHost.EnsureApplication();

            var window = new ThemeCreatorWindow();
            window.SourcePathTextBox.Text = ResolveBundledThemePalettePath();
            InvokeReloadThemes(window);
            WpfTestHost.ShowOffscreen(window);

            Assert.False(window.CreateButton.IsEnabled);

            window.ThemeIdTextBox.Text = "my-theme";
            WpfTestHost.DoEvents();

            Assert.True(window.CreateButton.IsEnabled);

            window.Close();
        });
    }

    [Fact]
    public void Window_NoSourceMode_HidesSourcePanel()
    {
        WpfTestHost.RunInSta(() =>
        {
            WpfTestHost.EnsureApplication();

            var window = new ThemeCreatorWindow();
            WpfTestHost.ShowOffscreen(window);

            Assert.Equal(Visibility.Visible, window.SourcePanel.Visibility);

            window.CreateWithoutSourceRadioButton.IsChecked = true;
            WpfTestHost.DoEvents();

            Assert.Equal(Visibility.Collapsed, window.SourcePanel.Visibility);
            Assert.True(window.BaseThemeComboBox.IsEnabled);

            window.Close();
        });
    }

    [Fact]
    public void FocusingThemeIdTextBox_DoesNotChangeLayout()
    {
        WpfTestHost.RunInSta(() =>
        {
            WpfTestHost.EnsureApplication();

            var window = new ThemeCreatorWindow();
            WpfTestHost.ShowOffscreen(window);

            var initialHeight = window.ThemeIdTextBox.ActualHeight;
            var initialOutputTop = window.OutputPathTextBox.TranslatePoint(new Point(0, 0), window).Y;

            window.ThemeIdTextBox.Focus();
            WpfTestHost.DoEvents();

            Assert.True(window.ThemeIdTextBox.IsKeyboardFocusWithin);
            Assert.Equal(initialHeight, window.ThemeIdTextBox.ActualHeight, 3);
            Assert.Equal(initialOutputTop, window.OutputPathTextBox.TranslatePoint(new Point(0, 0), window).Y, 3);

            window.Close();
        });
    }

    [Fact]
    public void FocusingBaseThemeComboBox_DoesNotChangeLayout()
    {
        WpfTestHost.RunInSta(() =>
        {
            WpfTestHost.EnsureApplication();

            var window = new ThemeCreatorWindow();
            WpfTestHost.ShowOffscreen(window);
            window.CreateWithoutSourceRadioButton.IsChecked = true;
            WpfTestHost.DoEvents();

            var initialHeight = window.BaseThemeComboBox.ActualHeight;
            var initialCreateTop = window.CreateButton.TranslatePoint(new Point(0, 0), window).Y;

            window.BaseThemeComboBox.Focus();
            WpfTestHost.DoEvents();

            Assert.True(window.BaseThemeComboBox.IsKeyboardFocusWithin);
            Assert.Equal(initialHeight, window.BaseThemeComboBox.ActualHeight, 3);
            Assert.Equal(initialCreateTop, window.CreateButton.TranslatePoint(new Point(0, 0), window).Y, 3);

            window.Close();
        });
    }

    private static string ResolveBundledThemePalettePath()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            var solutionPath = Path.Combine(directory.FullName, "applanch.slnx");
            if (File.Exists(solutionPath))
            {
                return Path.Combine(directory.FullName, "src", "applanch", "Config", "theme-palette.json");
            }

            directory = directory.Parent;
        }

        throw new InvalidOperationException("Workspace root could not be resolved for ThemeCreatorWindowTests.");
    }

    private static void InvokeReloadThemes(ThemeCreatorWindow window)
    {
        typeof(ThemeCreatorWindow)
            .GetMethod("ReloadThemes", BindingFlags.Instance | BindingFlags.NonPublic)!
            .Invoke(window, null);

        WpfTestHost.DoEvents();
    }
}