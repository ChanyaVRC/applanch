using System.Windows;
using applanch.Infrastructure.Storage;
using applanch.Infrastructure.Theming;

namespace applanch;

public partial class SettingsWindow : Window
{
    private SettingsWindowViewModel ViewModel { get; }

    public SettingsWindow(Window owner)
    {
        InitializeComponent();
        Owner = owner;
        ViewModel = new SettingsWindowViewModel(
            AppSettings.Load(),
            () => ((App)Application.Current).ReapplyTheme());
        DataContext = ViewModel;
    }

    private void Window_SourceInitialized(object? sender, EventArgs e) =>
        WindowCaptionThemeHelper.Apply(this);

    public bool SettingsChanged => ViewModel.SettingsChanged;

    internal AppSettings? SavedSettings => ViewModel.SavedSettings;
}
