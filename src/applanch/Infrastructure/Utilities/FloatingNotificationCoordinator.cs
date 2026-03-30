using System.Windows;

namespace applanch.Infrastructure.Utilities;

internal sealed class FloatingNotificationCoordinator
{
    private int _animationVersion;
    private int _expectedHideVersion;

    internal void BeginShow()
    {
        _animationVersion++;
    }

    internal bool BeginHide(bool isBannerVisible)
    {
        _expectedHideVersion = ++_animationVersion;
        return isBannerVisible;
    }

    internal bool CanCompleteHide()
    {
        return _expectedHideVersion == _animationVersion;
    }

    internal static NotificationIconType MapIcon(MessageBoxImage icon)
    {
        return icon switch
        {
            MessageBoxImage.Error => NotificationIconType.Error,
            MessageBoxImage.Warning => NotificationIconType.Warning,
            MessageBoxImage.Information => NotificationIconType.Info,
            _ => NotificationIconType.None,
        };
    }
}
