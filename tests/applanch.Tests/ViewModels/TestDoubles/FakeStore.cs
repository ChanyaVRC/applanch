using applanch.Infrastructure.Storage;

namespace applanch.Tests.ViewModels.TestDoubles;

internal sealed class FakeStore(IReadOnlyList<LauncherEntry> entries) : ILauncherStore
{
    private readonly List<LauncherEntry> _entries = entries.ToList();

    public int SaveCallCount { get; private set; }

    public IReadOnlyList<LauncherEntry> LastSavedEntries { get; private set; } = [];

    public IReadOnlyList<LauncherEntry> LoadAll() => _entries;

    public void SaveAll(IEnumerable<LauncherEntry> entries)
    {
        SaveCallCount++;
        LastSavedEntries = entries.ToList();
        _entries.Clear();
        _entries.AddRange(LastSavedEntries);
    }
}
