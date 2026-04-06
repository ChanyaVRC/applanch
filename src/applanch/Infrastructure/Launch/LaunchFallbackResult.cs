using System.Diagnostics;

namespace applanch.Infrastructure.Launch;

internal sealed record LaunchFallbackResult(ProcessStartInfo StartInfo, string Name);