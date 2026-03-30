using System.Windows;
using System.Windows.Media;

namespace applanch;

public partial class MessageDialogWindow : Window
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

        SourceInitialized += (_, _) => WindowCaptionThemeHelper.Apply(this);
        OkButton.Click += (_, _) => DialogResult = true;
        Loaded += (_, _) => OkButton.Focus();
    }
}
