namespace applanch;

internal interface ILauncherStore
{
    IReadOnlyList<LauncherStore.LauncherEntry> LoadAll();
    void SaveAll(IEnumerable<LauncherStore.LauncherEntry> entries);
}

internal sealed class LauncherStoreAdapter : ILauncherStore
{
    public IReadOnlyList<LauncherStore.LauncherEntry> LoadAll() => LauncherStore.LoadAll();

    public void SaveAll(IEnumerable<LauncherStore.LauncherEntry> entries) =>
        LauncherStore.SaveAll(entries);
}
