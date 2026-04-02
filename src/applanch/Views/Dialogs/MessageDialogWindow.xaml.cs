using System.Windows;
using System.Windows.Media;
using applanch.Infrastructure.Dialogs;
using applanch.Infrastructure.Theming;

namespace applanch;

public sealed partial class MessageDialogWindow : Window
{
    public MessageDialogWindow(string message, string caption, MessageBoxImage icon, Window? owner = null)
    {
        InitializeComponent();

        Title = caption;
        Owner = owner;
        WindowStartupLocation = owner is null
            ? WindowStartupLocation.CenterScreen
            : WindowStartupLocation.CenterOwner;

        MessageText.Text = message;

        var visual = MessageDialogVisuals.Resolve(icon);
        IconText.Text = visual.Symbol;

        if (!visual.ShowIcon)
        {
            IconBadge.Visibility = Visibility.Collapsed;
            IconSpacerColumn.Width = new GridLength(0);
        }

        if (TryFindResource(visual.BrushResourceKey) is Brush brush)
        {
            IconText.Foreground = brush;
        }

    }

    private void Window_SourceInitialized(object? sender, EventArgs e) =>
        WindowCaptionThemeHelper.Apply(this);

    private void Window_Loaded(object sender, RoutedEventArgs e) =>
        OkButton.Focus();

    private void OkButton_Click(object sender, RoutedEventArgs e) =>
        DialogResult = true;
}
