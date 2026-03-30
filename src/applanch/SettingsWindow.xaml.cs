using System.Windows;

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
        DebugUpdateCheckBox.IsChecked = _settings.DebugUpdate;
        _isInitializing = false;
        SourceInitialized += (_, _) => WindowCaptionThemeHelper.Apply(this);
    }

    public bool SettingsChanged { get; private set; }

    internal AppSettings? SavedSettings { get; private set; }

    private void DebugUpdateCheckBox_Changed(object sender, RoutedEventArgs e)
    {
        if (_isInitializing)
        {
            return;
        }

        var updated = _settings with
        {
            DebugUpdate = DebugUpdateCheckBox.IsChecked == true,
        };

        if (updated == _settings)
        {
            return;
        }

        updated.Save();
        _settings = updated;
        SavedSettings = updated;
        SettingsChanged = true;
    }
}
