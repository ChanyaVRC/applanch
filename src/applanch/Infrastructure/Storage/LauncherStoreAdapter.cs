namespace applanch;

internal sealed class LauncherStoreAdapter : ILauncherStore
{
    public IReadOnlyList<LauncherStore.LauncherEntry> LoadAll() => LauncherStore.LoadAll();

    public void SaveAll(IEnumerable<LauncherStore.LauncherEntry> entries) =>
        LauncherStore.SaveAll(entries);
}
