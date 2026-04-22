namespace applanch.ThemeCreator;

public partial class ThemePreviewWindow : System.Windows.Window
{
    public ThemePreviewWindow()
    {
        InitializeComponent();
    }

    internal void SetPreviewText(string text)
    {
        PreviewTextBox.Text = text;
    }
}
