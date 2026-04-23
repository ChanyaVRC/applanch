using applanch.Theming;
using System.ComponentModel;
using System.Runtime.CompilerServices;
namespace applanch.ThemeCreator;

internal sealed class ThemeCreatorEditableEntry : INotifyPropertyChanged
{
    private string _hex;
    private string _previewBrushHex = string.Empty;
    private System.Windows.Media.Brush _previewBrush = System.Windows.Media.Brushes.Transparent;

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

    public System.Windows.Media.Brush PreviewBrush
    {
        get
        {
            RefreshPreviewBrush();
            return _previewBrush;
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    private void RefreshPreviewBrush()
    {
        if (string.Equals(_previewBrushHex, _hex, StringComparison.Ordinal))
        {
            return;
        }

        _previewBrushHex = _hex;
        _previewBrush = CreateFrozenPreviewBrush(_hex);
    }

    private static System.Windows.Media.SolidColorBrush CreateFrozenPreviewBrush(string hex)
    {
        if (!ThemeColor.TryParse(hex, out var color))
        {
            return System.Windows.Media.Brushes.Transparent;
        }

        var brush = new System.Windows.Media.SolidColorBrush(color.ToMediaColor());
        brush.Freeze();
        return brush;
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}
