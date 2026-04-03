namespace applanch.Tests.Infrastructure.Integration.TestDoubles;

internal sealed class RecordingExplorerCommandRegistrar
{
    public List<string> Calls { get; } = [];

    public void Register(string shellExtensionComHostPath)
    {
        Calls.Add(shellExtensionComHostPath);
    }
}