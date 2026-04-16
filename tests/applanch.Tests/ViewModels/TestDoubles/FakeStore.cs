using applanch.Infrastructure.Storage;

namespace applanch.Tests.ViewModels.TestDoubles;

internal sealed class FakeStore : ILauncherStore
{
    private readonly List<LauncherEntry> _entries;

    public FakeStore() : this(null)
    {
    }

    public FakeStore(IReadOnlyList<LauncherEntry>? entries)
    {
        _entries = (entries ?? [
            new LauncherEntry(new LaunchPath(@"C:\Tools\App.exe"), Category.Default, string.Empty, "App")
        ]).ToList();
    }

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
