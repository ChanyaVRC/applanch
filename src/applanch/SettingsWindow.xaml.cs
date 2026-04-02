using System.Windows;
using applanch.Infrastructure.Storage;
using applanch.Infrastructure.Theming;

namespace applanch;

public sealed partial class SettingsWindow : Window
{
    private readonly AppEvent _appEvent;
    private SettingsWindowViewModel ViewModel { get; }

    public SettingsWindow(Window owner)
    {
        InitializeComponent();
        Owner = owner;
        _appEvent = ((App)Application.Current).Events;
        ViewModel = new SettingsWindowViewModel(
            AppSettings.Load(),
            _appEvent);
        _appEvent.Register(AppEvents.Refresh, OnAppRefreshRequested);
        DataContext = ViewModel;
    }

    protected override void OnClosed(EventArgs e)
    {
        _appEvent.Unregister(AppEvents.Refresh, OnAppRefreshRequested);
        base.OnClosed(e);
    }

    private void Window_SourceInitialized(object? sender, EventArgs e) =>
        WindowCaptionThemeHelper.Apply(this);

    private void ResetToDefaults_Click(object sender, RoutedEventArgs e) =>
        ViewModel.ResetToDefaults();

    private void OnAppRefreshRequested(AppSettings settings)
    {
        if (!Dispatcher.CheckAccess())
        {
            Dispatcher.Invoke(() => ViewModel.ApplyExternalSettings(settings));
            return;
        }

        ViewModel.ApplyExternalSettings(settings);
    }

    public bool SettingsChanged => ViewModel.SettingsChanged;
}
