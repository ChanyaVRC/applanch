namespace applanch.Tests.Infrastructure.Integration.TestDoubles;

internal sealed class RecordingRegistryKeyDeleter
{
    public List<string> Calls { get; } = [];

    public void Delete(string keyPath)
    {
        Calls.Add(keyPath);
    }
}
