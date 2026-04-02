using System.Windows.Media;
using applanch.Infrastructure.Storage;

namespace applanch.Infrastructure.Integration;

internal interface ILaunchItemIconProvider
{
    void ApplySettings(AppSettings settings);

    ImageSource? GetInitialIcon(string fullPath);

    ValueTask<ImageSource?> GetDeferredIconAsync(string fullPath);
}