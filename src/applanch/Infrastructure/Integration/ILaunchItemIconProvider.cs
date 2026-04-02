using System.Windows.Media;

namespace applanch.Infrastructure.Integration;

internal interface ILaunchItemIconProvider
{
    ImageSource? GetInitialIcon(string fullPath);

    ValueTask<ImageSource?> GetDeferredIconAsync(string fullPath);
}