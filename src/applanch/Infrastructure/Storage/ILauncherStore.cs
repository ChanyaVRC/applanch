namespace applanch.Infrastructure.Storage;

internal interface ILauncherStore
{
    IReadOnlyList<LauncherEntry> LoadAll();
    void SaveAll(IEnumerable<LauncherEntry> entries);
}

