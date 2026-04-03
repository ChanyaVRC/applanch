using System.Diagnostics;
using applanch.Infrastructure.Utilities;

namespace applanch.Infrastructure.Launch;

internal interface ILaunchFallbackResolver
{
    bool TryCreatePreferred(
        LaunchPath launchPath,
        bool runAsAdministrator,
        out ProcessStartInfo fallback,
        out string fallbackName);

    bool TryCreate(
        LaunchPath launchPath,
        bool runAsAdministrator,
        out ProcessStartInfo fallback,
        out string fallbackName);
}
