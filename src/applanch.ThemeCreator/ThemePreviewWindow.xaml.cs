using applanch.Theming;

namespace applanch.ThemeCreator;

public partial class ThemePreviewWindow : DialogWindowBase
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
