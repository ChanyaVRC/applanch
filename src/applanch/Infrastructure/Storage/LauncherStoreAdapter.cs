namespace applanch.Infrastructure.Storage;

internal sealed class LauncherStoreAdapter : ILauncherStore
{
    public IReadOnlyList<LauncherEntry> LoadAll() => LauncherStore.LoadAll();

    public void SaveAll(IEnumerable<LauncherEntry> entries) =>
        LauncherStore.SaveAll(entries);
}

