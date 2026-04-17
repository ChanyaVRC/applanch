using System.Diagnostics;
using applanch.Infrastructure.Launch;

namespace applanch.Tests.Infrastructure.Launch.TestDoubles;

internal sealed class DelegateProcessStarter(Func<ProcessStartInfo, Process?> start) : IProcessStarter
{
    public Process? Start(ProcessStartInfo startInfo) => start(startInfo);
}
