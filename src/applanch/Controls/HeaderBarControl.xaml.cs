using System.Windows;
using System.Windows.Controls;
using applanch.Infrastructure.Updates;
using applanch.Infrastructure.Utilities;

namespace applanch.Controls;

public sealed partial class HeaderBarControl : UserControl
{
    private static readonly string DefaultAppVersionText = $"v{AppVersionProvider.CurrentVersion}";

    public HeaderBarControl()
    {
        InitializeComponent();
    }

    public static readonly DependencyProperty UpdateButtonVisibilityProperty =
        DependencyProperty.Register(
            nameof(UpdateButtonVisibility),
            typeof(Visibility),
            typeof(HeaderBarControl),
            new PropertyMetadata(Visibility.Collapsed));

    public Visibility UpdateButtonVisibility
    {
        get => (Visibility)GetValue(UpdateButtonVisibilityProperty);
        set => SetValue(UpdateButtonVisibilityProperty, value);
    }

    public static readonly DependencyProperty IsUpdateButtonEnabledProperty =
        DependencyProperty.Register(
            nameof(IsUpdateButtonEnabled),
            typeof(bool),
            typeof(HeaderBarControl),
            new PropertyMetadata(true));

    public bool IsUpdateButtonEnabled
    {
        get => (bool)GetValue(IsUpdateButtonEnabledProperty);
        set => SetValue(IsUpdateButtonEnabledProperty, value);
    }

    public static readonly DependencyProperty AppVersionTextProperty =
        DependencyProperty.Register(
            nameof(AppVersionText),
            typeof(string),
            typeof(HeaderBarControl),
            new PropertyMetadata(DefaultAppVersionText, OnAppVersionTextChanged));

    private static readonly DependencyPropertyKey IsPrereleaseVersionPropertyKey =
        DependencyProperty.RegisterReadOnly(
            nameof(IsPrereleaseVersion),
            typeof(bool),
            typeof(HeaderBarControl),
            new PropertyMetadata(IsPrereleaseVersionText(DefaultAppVersionText)));

    public static readonly DependencyProperty IsPrereleaseVersionProperty = IsPrereleaseVersionPropertyKey.DependencyProperty;

    public string AppVersionText
    {
        get => (string)GetValue(AppVersionTextProperty);
        set => SetValue(AppVersionTextProperty, value);
    }

    public bool IsPrereleaseVersion => (bool)GetValue(IsPrereleaseVersionProperty);

    public event RoutedEventHandler? UpdateRequested;

    public event RoutedEventHandler? SettingsRequested;

    private void UpdateButton_Click(object sender, RoutedEventArgs e)
    {
        UpdateRequested?.Invoke(this, e);
    }

    private void SettingsButton_Click(object sender, RoutedEventArgs e)
    {
        SettingsRequested?.Invoke(this, e);
    }

    private static void OnAppVersionTextChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not HeaderBarControl control)
        {
            return;
        }

        control.SetValue(IsPrereleaseVersionPropertyKey, IsPrereleaseVersionText(e.NewValue as string));
    }

    private static bool IsPrereleaseVersionText(string? appVersionText)
    {
        if (string.IsNullOrWhiteSpace(appVersionText))
        {
            return false;
        }

        var normalized = appVersionText.Trim();
        if (normalized.StartsWith('v') || normalized.StartsWith('V'))
        {
            normalized = normalized[1..];
        }

        return SemanticVersion.TryParse(normalized, out var version) && version.IsPrerelease;
    }
}
