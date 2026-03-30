using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;

namespace applanch;

public sealed class FloatingNotificationState : INotifyPropertyChanged
{
    private string _message = string.Empty;
    private NotificationIconType _iconType;
    private Action? _undoAction;

    public event PropertyChangedEventHandler? PropertyChanged;

    public string Message
    {
        get => _message;
        internal set => SetField(ref _message, value);
    }

    public NotificationIconType IconType
    {
        get => _iconType;
        internal set => SetField(ref _iconType, value);
    }

    public Action? UndoAction
    {
        get => _undoAction;
        internal set
        {
            _undoAction = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(UndoAction)));
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(UndoVisibility)));
        }
    }

    public Visibility UndoVisibility => _undoAction is null ? Visibility.Collapsed : Visibility.Visible;

    private bool SetField<T>(ref T field, T value, [CallerMemberName] string propertyName = "")
    {
        if (Equals(field, value))
        {
            return false;
        }

        field = value;
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        return true;
    }
}
