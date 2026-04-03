namespace applanch.Tests.Infrastructure.Integration.TestDoubles;

internal sealed class RecordingRegistryCommandWriter
{
    public List<(string KeyPath, string MenuText, string IconPath, string Command, bool EnableExplorerCommand)> Calls { get; } = [];

    public void WriteCommand(string keyPath, string menuText, string iconPath, string command, bool enableExplorerCommand)
    {
        Calls.Add((keyPath, menuText, iconPath, command, enableExplorerCommand));
    }
}
