using System.Windows;
using System.Windows.Controls;

namespace applanch.Controls;

public sealed partial class UpdateBannerControl : UserControl
{
    public UpdateBannerControl()
    {
        InitializeComponent();
    }

    public static readonly DependencyProperty MessageProperty =
        DependencyProperty.Register(
            nameof(Message),
            typeof(string),
            typeof(UpdateBannerControl),
            new PropertyMetadata(string.Empty));

    public string Message
    {
        get => (string)GetValue(MessageProperty);
        set => SetValue(MessageProperty, value);
    }

    public event RoutedEventHandler? UpdateRequested;

    public event RoutedEventHandler? DismissRequested;

    private void UpdateButton_Click(object sender, RoutedEventArgs e)
    {
        UpdateRequested?.Invoke(this, e);
    }

    private void DismissButton_Click(object sender, RoutedEventArgs e)
    {
        DismissRequested?.Invoke(this, e);
    }
}
