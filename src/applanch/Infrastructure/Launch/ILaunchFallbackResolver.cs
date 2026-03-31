using System.Diagnostics;

namespace applanch.Infrastructure.Launch;

internal interface ILaunchFallbackResolver
{
    bool TryCreate(
        string launchPath,
        bool runAsAdministrator,
        out ProcessStartInfo fallback,
        out string fallbackName);
}
