using System.Windows;
using System.Windows.Controls;
using applanch.Infrastructure.Storage;
using applanch.Infrastructure.Theming;

namespace applanch;

public partial class SettingsWindow : Window
{
    private AppSettings _settings;
    private bool _isInitializing;

    public SettingsWindow(Window owner)
    {
        InitializeComponent();
        Owner = owner;
        _settings = AppSettings.Load();
        _isInitializing = true;
        ThemeComboBox.SelectedIndex = (int)_settings.Theme;
        CloseOnLaunchCheckBox.IsChecked = _settings.CloseOnLaunch;
        CheckForUpdatesCheckBox.IsChecked = _settings.CheckForUpdatesOnStartup;
        DebugUpdateCheckBox.IsChecked = _settings.DebugUpdate;
        _isInitializing = false;
        SourceInitialized += (_, _) => WindowCaptionThemeHelper.Apply(this);
    }

    public bool SettingsChanged { get; private set; }

    internal AppSettings? SavedSettings { get; private set; }

    private void ThemeComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_isInitializing) return;
        ApplySetting(s => s with { Theme = (AppTheme)ThemeComboBox.SelectedIndex });
        ((App)Application.Current).ReapplyTheme();
    }

    private void CloseOnLaunchCheckBox_Changed(object sender, RoutedEventArgs e)
    {
        if (_isInitializing) return;
        ApplySetting(s => s with { CloseOnLaunch = CloseOnLaunchCheckBox.IsChecked == true });
    }

    private void CheckForUpdatesCheckBox_Changed(object sender, RoutedEventArgs e)
    {
        if (_isInitializing) return;
        ApplySetting(s => s with { CheckForUpdatesOnStartup = CheckForUpdatesCheckBox.IsChecked == true });
    }

    private void DebugUpdateCheckBox_Changed(object sender, RoutedEventArgs e)
    {
        if (_isInitializing) return;
        ApplySetting(s => s with { DebugUpdate = DebugUpdateCheckBox.IsChecked == true });
    }

    private void ApplySetting(Func<AppSettings, AppSettings> update)
    {
        var updated = update(_settings);
        if (updated == _settings) return;
        updated.Save();
        _settings = updated;
        SavedSettings = updated;
        SettingsChanged = true;
    }
}
