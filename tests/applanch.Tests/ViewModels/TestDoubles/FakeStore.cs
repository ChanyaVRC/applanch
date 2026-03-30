using applanch.Infrastructure.Storage;

namespace applanch.Tests.ViewModels.TestDoubles;

internal sealed class FakeStore(IReadOnlyList<LauncherStore.LauncherEntry> entries) : ILauncherStore
{
    private readonly List<LauncherStore.LauncherEntry> _entries = entries.ToList();

    public int SaveCallCount { get; private set; }

    public IReadOnlyList<LauncherStore.LauncherEntry> LastSavedEntries { get; private set; } = [];

    public IReadOnlyList<LauncherStore.LauncherEntry> LoadAll() => _entries;

    public void SaveAll(IEnumerable<LauncherStore.LauncherEntry> entries)
    {
        SaveCallCount++;
        LastSavedEntries = entries.ToList();
        _entries.Clear();
        _entries.AddRange(LastSavedEntries);
    }
}
