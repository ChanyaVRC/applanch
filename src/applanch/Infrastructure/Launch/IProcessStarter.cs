using System.Diagnostics;

namespace applanch.Infrastructure.Launch;

internal interface IProcessStarter
{
    Process? Start(ProcessStartInfo startInfo);
}
