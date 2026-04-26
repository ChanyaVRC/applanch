using applanch.Core.ViewModels;
using applanch.Theming;
using Brush = System.Windows.Media.Brush;
using Brushes = System.Windows.Media.Brushes;
using SolidColorBrush = System.Windows.Media.SolidColorBrush;

namespace applanch.ThemeCreator;

internal sealed class ThemeCreatorEditableEntry : ObservableObject
{
    private ThemeColor? _color;
    private ThemeColor? _cachedBrushColor;
    private Brush _previewBrush = Brushes.Transparent;

    internal ThemeCreatorEditableEntry(string key, string description, ThemeColor? color)
    {
        Key = key;
        Description = description;
        _color = color;
    }

    public string Key { get; }

    public string Description { get; }

    public ThemeColor? Color
    {
        get => _color;
        set
        {
            if (_color == value)
                return;

            _color = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(Hex));
            OnPropertyChanged(nameof(PreviewBrush));
        }
    }

    public string Hex
    {
        get => _color?.Hex ?? string.Empty;
        set
        {
            var newColor = ThemeColor.TryParse(value, out var parsed) ? parsed : (ThemeColor?)null;
            if (_color == newColor)
                return;

            _color = newColor;
            OnPropertyChanged();
            OnPropertyChanged(nameof(Color));
            OnPropertyChanged(nameof(PreviewBrush));
        }
    }

    public Brush PreviewBrush
    {
        get
        {
            if (_cachedBrushColor != _color)
            {
                _cachedBrushColor = _color;
                _previewBrush = _color is { } c ? CreateFrozenBrush(c) : Brushes.Transparent;
            }

            return _previewBrush;
        }
    }

    private static SolidColorBrush CreateFrozenBrush(ThemeColor color)
    {
        var brush = new SolidColorBrush(color.ToMediaColor());
        brush.Freeze();
        return brush;
    }
}

