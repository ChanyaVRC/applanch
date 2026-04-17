using System.Diagnostics;

namespace applanch.Infrastructure.Launch;

internal sealed class ProcessStarter : IProcessStarter
{
    public Process? Start(ProcessStartInfo startInfo) => Process.Start(startInfo);
}
