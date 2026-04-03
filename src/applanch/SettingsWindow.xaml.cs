using System.Diagnostics;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows;
using applanch.Events;
using applanch.Infrastructure.Storage;
using applanch.Infrastructure.Theming;
using applanch.Infrastructure.Utilities;
using applanch.ViewModels;

namespace applanch;

public sealed partial class SettingsWindow : Window
{
    private static readonly Uri ReportBugIssueBaseUri = new("https://github.com/ChanyaVRC/applanch/issues/new", UriKind.Absolute);

    private readonly AppEvent _appEvent;
    private SettingsWindowViewModel ViewModel { get; }

    internal SettingsWindow(Window owner, AppSettings settings)
    {
        InitializeComponent();
        Owner = owner;
        _appEvent = ((App)Application.Current).Events;
        ViewModel = new SettingsWindowViewModel(
            settings,
            _appEvent);
        _appEvent.Register(AppEvents.Refresh, OnAppRefreshRequested);
        DataContext = ViewModel;
    }

    protected override void OnClosed(EventArgs e)
    {
        _appEvent.Unregister(AppEvents.Refresh, OnAppRefreshRequested);
        base.OnClosed(e);
    }

    private void Window_SourceInitialized(object? sender, EventArgs e)
    {
        WindowCaptionThemeHelper.Apply(this);
        WindowIconThemeHelper.Apply(this, Application.Current.Resources);
    }

    private void ResetToDefaults_Click(object sender, RoutedEventArgs e) =>
        ViewModel.ResetToDefaults();

    private void CopyDiagnostics_Click(object sender, RoutedEventArgs e)
    {
        Clipboard.SetText(CreateDiagnosticsText(ViewModel));
        MessageBox.Show(
            LocalizedStrings.Instance[nameof(AppResources.Notification_DiagnosticsCopied)],
            LocalizedStrings.Instance[nameof(AppResources.Window_Settings)],
            MessageBoxButton.OK,
            MessageBoxImage.Information);
    }

    private void OpenLogFolder_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            if (Process.Start(CreateOpenLogFolderStartInfo()) is null)
            {
                MessageBox.Show(
                    LocalizedStrings.Instance[nameof(AppResources.Error_OpenLogFolder)],
                    LocalizedStrings.Instance[nameof(AppResources.Window_Settings)],
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
            }
        }
        catch (Exception ex)
        {
            AppLogger.Instance.Warn($"Failed to open log folder '{AppLogger.LogDirectoryPath}': {ex.Message}");
            MessageBox.Show(
                LocalizedStrings.Instance[nameof(AppResources.Error_OpenLogFolder)],
                LocalizedStrings.Instance[nameof(AppResources.Window_Settings)],
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
        }
    }

    private void ReportBug_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            if (Process.Start(CreateReportBugStartInfo()) is null)
            {
                MessageBox.Show(
                    LocalizedStrings.Instance[nameof(AppResources.Error_OpenBugReport)],
                    LocalizedStrings.Instance[nameof(AppResources.Window_Settings)],
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
            }
        }
        catch (Exception ex)
        {
            AppLogger.Instance.Warn($"Failed to open bug report URL '{ReportBugIssueBaseUri}': {ex.Message}");
            MessageBox.Show(
                LocalizedStrings.Instance[nameof(AppResources.Error_OpenBugReport)],
                LocalizedStrings.Instance[nameof(AppResources.Window_Settings)],
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
        }
    }

    internal static ProcessStartInfo CreateReportBugStartInfo() => new()
    {
        UseShellExecute = true,
        FileName = CreateReportBugIssueUri().AbsoluteUri,
    };

    internal static Uri CreateReportBugIssueUri()
    {
        var title = Uri.EscapeDataString(AppResources.BugReport_IssueTitle);
        var body = Uri.EscapeDataString(CreateReportBugBody());
        return new Uri($"{ReportBugIssueBaseUri.AbsoluteUri}?title={title}&body={body}", UriKind.Absolute);
    }

    internal static string CreateReportBugBody()
    {
        var appVersion = AppVersionProvider.GetDisplayVersion();

        return string.Format(
            CultureInfo.CurrentCulture,
            AppResources.BugReport_IssueBodyTemplate,
            appVersion,
            RuntimeInformation.OSDescription.Trim(),
            RuntimeInformation.OSArchitecture,
            RuntimeInformation.ProcessArchitecture,
            RuntimeInformation.FrameworkDescription,
            CultureInfo.CurrentUICulture.Name,
            CultureInfo.CurrentCulture.Name);
    }

    internal static ProcessStartInfo CreateOpenLogFolderStartInfo() => new()
    {
        UseShellExecute = true,
        FileName = "explorer.exe",
        Arguments = $"/select,\"{AppLogger.LogFilePathValue}\"",
    };

    internal static string CreateDiagnosticsText(SettingsWindowViewModel settingsViewModel)
    {
        var builder = new StringBuilder(256);
        builder.AppendLine($"App version: {AppVersionProvider.GetDisplayVersion()}");
        builder.AppendLine($"OS: {RuntimeInformation.OSDescription.Trim()}");
        builder.AppendLine($".NET: {RuntimeInformation.FrameworkDescription}");
        builder.AppendLine($"UI culture: {CultureInfo.CurrentUICulture.Name}");
        builder.AppendLine($"Culture: {CultureInfo.CurrentCulture.Name}");
        builder.AppendLine($"Log folder: {AppLogger.LogDirectoryPath}");
        builder.AppendLine($"Update check on startup: {settingsViewModel.CheckForUpdatesOnStartup}");
        builder.AppendLine($"Update install behavior: {(UpdateInstallBehavior)settingsViewModel.UpdateInstallBehaviorIndex}");
        builder.AppendLine($"Debug update mode: {settingsViewModel.DebugUpdate}");
        return builder.ToString();
    }

    private void OnAppRefreshRequested(AppSettings settings)
    {
        Dispatcher.InvokeIfRequired(() => ViewModel.ApplyExternalSettings(settings));
    }

    public bool SettingsChanged => ViewModel.SettingsChanged;
}
