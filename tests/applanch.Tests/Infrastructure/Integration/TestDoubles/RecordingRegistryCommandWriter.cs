namespace applanch.Tests.Infrastructure.Integration.TestDoubles;

internal sealed class RecordingRegistryCommandWriter
{
    public List<(string KeyPath, string MenuText, string IconPath, string Command)> Calls { get; } = [];

    public void WriteCommand(string keyPath, string menuText, string iconPath, string command)
    {
        Calls.Add((keyPath, menuText, iconPath, command));
    }
}
