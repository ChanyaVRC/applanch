using System.Windows;

namespace applanch;

public partial class SettingsWindow : Window
{
    private readonly AppSettings _settings;

    public SettingsWindow(Window owner)
    {
        InitializeComponent();
        Owner = owner;
        _settings = AppSettings.Load();
        DebugUpdateCheckBox.IsChecked = _settings.DebugUpdate;
        SourceInitialized += (_, _) => WindowCaptionThemeHelper.Apply(this);
        SaveButton.Click += SaveButton_Click;
    }

    public bool SettingsChanged { get; private set; }

    private void SaveButton_Click(object sender, RoutedEventArgs e)
    {
        var updated = _settings with
        {
            DebugUpdate = DebugUpdateCheckBox.IsChecked == true,
        };

        if (updated != _settings)
        {
            updated.Save();
            SettingsChanged = true;
        }

        DialogResult = true;
    }
}
