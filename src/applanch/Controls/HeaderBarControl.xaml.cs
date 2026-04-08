using System.Windows;
using System.Windows.Controls;
using applanch.Infrastructure.Utilities;

namespace applanch.Controls;

public sealed partial class HeaderBarControl : UserControl
{
    private static readonly string DefaultAppVersionText = $"v{AppVersionProvider.GetDisplayVersion()}";

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

    public static readonly DependencyProperty AppVersionTextProperty =
        DependencyProperty.Register(
            nameof(AppVersionText),
            typeof(string),
            typeof(HeaderBarControl),
            new PropertyMetadata(DefaultAppVersionText));

    public string AppVersionText
    {
        get => (string)GetValue(AppVersionTextProperty);
        set => SetValue(AppVersionTextProperty, value);
    }

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
}
