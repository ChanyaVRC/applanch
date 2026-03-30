namespace applanch.Infrastructure.Storage;

internal interface ILauncherStore
{
    IReadOnlyList<LauncherStore.LauncherEntry> LoadAll();
    void SaveAll(IEnumerable<LauncherStore.LauncherEntry> entries);
}

