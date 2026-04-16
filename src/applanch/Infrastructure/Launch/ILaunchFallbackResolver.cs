using applanch.Core.Utilities;

namespace applanch.Infrastructure.Launch;

internal interface ILaunchFallbackResolver
{
    LaunchFallbackResult? TryCreatePreferred(
        LaunchPath launchPath,
        bool runAsAdministrator);

    LaunchFallbackResult? TryCreate(
        LaunchPath launchPath,
        bool runAsAdministrator);
}