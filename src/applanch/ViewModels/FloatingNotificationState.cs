using System.Windows;

namespace applanch;

public sealed class FloatingNotificationState : ObservableObject
{
    private string _message = string.Empty;
    private NotificationIconType _iconType;
    private Action? _undoAction;

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
            if (SetField(ref _undoAction, value))
            {
                OnPropertyChanged(nameof(UndoVisibility));
            }
        }
    }

    public Visibility UndoVisibility => _undoAction is null ? Visibility.Collapsed : Visibility.Visible;
}
