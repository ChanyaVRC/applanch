using System.Windows;
using System.Windows.Media;
using applanch.Infrastructure.Dialogs;
using applanch.Infrastructure.Theming;

namespace applanch.Views.Dialogs;

public sealed partial class MessageDialogWindow : Window
{
    public string DialogMessage { get; }

    public string DialogIconSymbol { get; }

    public Brush DialogIconBrush { get; }

    public Visibility DialogIconVisibility { get; }

    public MessageDialogWindow(string message, string caption, MessageBoxImage icon, Window? owner = null)
    {
        InitializeComponent();

        Title = caption;
        Owner = owner;
        WindowStartupLocation = owner is null
            ? WindowStartupLocation.CenterScreen
            : WindowStartupLocation.CenterOwner;

        DialogMessage = message;

        var visual = MessageDialogVisuals.Resolve(icon);
        DialogIconSymbol = visual.Symbol;
        DialogIconVisibility = visual.ShowIcon ? Visibility.Visible : Visibility.Collapsed;
        IconSpacerColumn.Width = visual.ShowIcon ? new GridLength(12) : new GridLength(0);
        DialogIconBrush = TryFindResource(visual.BrushResourceKey) as Brush
            ?? (Brush)FindResource("Brush.TextSecondary");

        DataContext = this;

    }

    private void Window_SourceInitialized(object? sender, EventArgs e) =>
        WindowCaptionThemeHelper.Apply(this);

    private void Window_Loaded(object sender, RoutedEventArgs e) =>
        OkButton.Focus();

    private void OkButton_Click(object sender, RoutedEventArgs e) =>
        DialogResult = true;
}
