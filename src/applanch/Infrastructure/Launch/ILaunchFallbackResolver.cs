using System.Diagnostics;

namespace applanch.Infrastructure.Launch;

internal interface ILaunchFallbackResolver
{
    bool TryCreatePreferred(
        string launchPath,
        bool runAsAdministrator,
        out ProcessStartInfo fallback,
        out string fallbackName);

    bool TryCreate(
        string launchPath,
        bool runAsAdministrator,
        out ProcessStartInfo fallback,
        out string fallbackName);
}
