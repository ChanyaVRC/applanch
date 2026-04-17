using System.Windows.Media;
using applanch.Settings;
using applanch.Utilities;

namespace applanch.Infrastructure.Integration;

internal interface ILaunchItemIconProvider
{
    void ApplySettings(AppSettings settings);

    ImageSource? GetInitialIcon(LaunchPath path);

    ValueTask<ImageSource?> GetDeferredIconAsync(LaunchPath path);
}
