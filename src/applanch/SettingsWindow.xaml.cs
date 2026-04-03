using System.Diagnostics;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Windows;
using applanch.Events;
using applanch.Infrastructure.Dialogs;
using applanch.Infrastructure.Storage;
using applanch.Infrastructure.Theming;
using applanch.Infrastructure.Utilities;
using applanch.ViewModels;

namespace applanch;

public sealed partial class SettingsWindow : Window
{
    private static readonly Uri ReportBugIssueBaseUri = new("https://github.com/ChanyaVRC/applanch/issues/new", UriKind.Absolute);

    private readonly AppEvent _appEvent;
    private readonly IUserInteractionService _interactionService;
    private SettingsWindowViewModel ViewModel { get; }

    internal SettingsWindow(Window owner, AppSettings settings, IUserInteractionService interactionService)
    {
        InitializeComponent();
        Owner = owner;
        _appEvent = ((App)Application.Current).Events;
        _interactionService = interactionService;
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
        Clipboard.SetText(ViewModel.CreateDiagnosticsText());
        _interactionService.Show(
            LocalizedStrings.Instance[nameof(AppResources.Notification_DiagnosticsCopied)],
            LocalizedStrings.Instance[nameof(AppResources.Window_Settings)],
            MessageBoxImage.Information);
    }

    private void OpenLogFolder_Click(object sender, RoutedEventArgs e) =>
        TryStartProcess(
            CreateOpenLogFolderStartInfo(),
            AppLogger.LogDirectoryPath,
            LocalizedStrings.Instance[nameof(AppResources.Error_OpenLogFolder)]);

    private void ReportBug_Click(object sender, RoutedEventArgs e) =>
        TryStartProcess(
            CreateReportBugStartInfo(),
            ReportBugIssueBaseUri.AbsoluteUri,
            LocalizedStrings.Instance[nameof(AppResources.Error_OpenBugReport)]);

    private void TryStartProcess(ProcessStartInfo startInfo, string target, string errorMessage)
    {
        try
        {
            if (Process.Start(startInfo) is not null)
            {
                return;
            }

            AppLogger.Instance.Warn($"Failed to start external process for '{target}': Process.Start returned null.");
        }
        catch (Exception ex)
        {
            AppLogger.Instance.Warn($"Failed to start external process for '{target}': {ex.Message}");
        }

        _interactionService.Show(
            errorMessage,
            LocalizedStrings.Instance[nameof(AppResources.Window_Settings)],
            MessageBoxImage.Warning);
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

    private void OnAppRefreshRequested(AppSettings settings)
    {
        Dispatcher.InvokeIfRequired(() => ViewModel.ApplyExternalSettings(settings));
    }

    public bool SettingsChanged => ViewModel.SettingsChanged;
}
