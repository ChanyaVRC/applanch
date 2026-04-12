using System.Windows;
using System.Windows.Media;
using applanch.Infrastructure.Dialogs;

namespace applanch.Views.Dialogs;

public sealed partial class MessageDialogWindow : DialogWindowBase
{
    public string DialogMessage { get; }

    public string DialogIconSymbol { get; }

    public Brush DialogIconBrush { get; }

    public Visibility DialogIconVisibility { get; }

    public MessageDialogWindow(string message, string caption, MessageBoxImage icon, Window? owner = null)
    {
        InitializeComponent();

        InitializeDialogWindow(caption, owner);

        DialogMessage = message;

        var visual = MessageDialogVisuals.Resolve(icon);
        DialogIconSymbol = visual.Symbol;
        DialogIconVisibility = visual.ShowIcon ? Visibility.Visible : Visibility.Collapsed;
        IconSpacerColumn.Width = visual.ShowIcon ? new GridLength(12) : new GridLength(0);
        DialogIconBrush = TryFindResource(visual.BrushResourceKey) as Brush
            ?? Brushes.DimGray;

        DataContext = this;
    }

    protected override void FocusInitialElement() =>
        OkButton.Focus();

    private void OkButton_Click(object sender, RoutedEventArgs e) =>
        DialogResult = true;
}
