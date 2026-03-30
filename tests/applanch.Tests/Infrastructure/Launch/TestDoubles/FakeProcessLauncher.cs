using System.Diagnostics;

namespace applanch.Tests.Infrastructure.Launch.TestDoubles;

internal sealed class FakeProcessLauncher
{
    public bool ThrowOnStart { get; set; }
    public bool ReturnNull { get; set; }
    public ProcessStartInfo? LastStartInfo { get; private set; }

    public Process? Start(ProcessStartInfo startInfo)
    {
        LastStartInfo = startInfo;

        if (ThrowOnStart)
        {
            throw new InvalidOperationException("simulated");
        }

        if (ReturnNull)
        {
            return null;
        }

        return new Process();
    }
}
