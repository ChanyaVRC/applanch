namespace applanch.Infrastructure.Utilities;

internal static class FloatingNotificationProgressState
{
    internal static double CaptureVisibleScale(double scaleX)
    {
        if (double.IsNaN(scaleX) || double.IsInfinity(scaleX))
        {
            return 0;
        }

        return Math.Clamp(scaleX, 0, 1);
    }
}
