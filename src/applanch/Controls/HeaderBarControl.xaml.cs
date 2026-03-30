using System.Windows;
using System.Windows.Controls;

namespace applanch.Controls;

public partial class HeaderBarControl : UserControl
{
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
