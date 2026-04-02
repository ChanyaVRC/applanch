using System.Windows.Media;
using applanch.Infrastructure.Storage;
using applanch.Infrastructure.Utilities;

namespace applanch.Infrastructure.Integration;

internal interface ILaunchItemIconProvider
{
    void ApplySettings(AppSettings settings);

    ImageSource? GetInitialIcon(LaunchPath path);

    ValueTask<ImageSource?> GetDeferredIconAsync(LaunchPath path);
}