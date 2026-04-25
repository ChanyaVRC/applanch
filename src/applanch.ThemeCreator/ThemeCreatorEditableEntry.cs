using applanch.Core.ViewModels;
using applanch.Theming;
using Brush = System.Windows.Media.Brush;
using Brushes = System.Windows.Media.Brushes;
using SolidColorBrush = System.Windows.Media.SolidColorBrush;

namespace applanch.ThemeCreator;

internal sealed class ThemeCreatorEditableEntry : ObservableObject
{
    private string _hex;
    private string _previewBrushHex = string.Empty;
    private Brush _previewBrush = Brushes.Transparent;

    internal ThemeCreatorEditableEntry(string key, string description, string hex)
    {
        Key = key;
        Description = description;
        _hex = hex;
        RefreshPreviewBrush();
    }

    public string Key { get; }

    public string Description { get; }

    public string Hex
    {
        get => _hex;
        set
        {
            if (string.Equals(_hex, value, StringComparison.Ordinal))
            {
                return;
            }

            _hex = value;
            RefreshPreviewBrush();
            OnPropertyChanged();
            OnPropertyChanged(nameof(PreviewBrush));
        }
    }

    public Brush PreviewBrush
    {
        get
        {
            RefreshPreviewBrush();
            return _previewBrush;
        }
    }

    private void RefreshPreviewBrush()
    {
        if (string.Equals(_previewBrushHex, _hex, StringComparison.Ordinal))
        {
            return;
        }

        _previewBrushHex = _hex;
        _previewBrush = CreateFrozenPreviewBrush(_hex);
    }

    private static SolidColorBrush CreateFrozenPreviewBrush(string hex)
    {
        if (!ThemeColor.TryParse(hex, out var color))
        {
            return Brushes.Transparent;
        }

        var brush = new SolidColorBrush(color.ToMediaColor());
        brush.Freeze();
        return brush;
    }
}

