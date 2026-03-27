using System.Reflection;
using Xunit;

namespace applanch.Tests;

[Collection(nameof(LauncherStoreIoCollection))]
public class LauncherStoreBehaviorTests
{
    [Fact]
    public void LoadAll_NoStoreFiles_ReturnsEmpty()
    {
        using var scope = new StoreIsolationScope();

        var entries = LauncherStore.LoadAll();

        Assert.Empty(entries);
    }

    [Fact]
    public void Add_NewEntry_PersistsNormalizedValues()
    {
        using var scope = new StoreIsolationScope();

        LauncherStore.Add("  C:\\Tools\\MyApp.exe  ", "  Dev  ", "  --run  ", "  My App  ");

        var entries = LauncherStore.LoadAll();
        var entry = Assert.Single(entries);
        Assert.Equal(Path.GetFullPath(@"C:\Tools\MyApp.exe"), entry.Path);
        Assert.Equal("Dev", entry.Category);
        Assert.Equal("--run", entry.Arguments);
        Assert.Equal("My App", entry.DisplayName);
    }

    [Fact]
    public void Add_DuplicatePath_IgnoresSecondEntry()
    {
        using var scope = new StoreIsolationScope();

        LauncherStore.Add(@"C:\Tools\Dupe.exe", "Dev", "--first", "First");
        LauncherStore.Add(@"c:\tools\dupe.exe", "Ops", "--second", "Second");

        var entries = LauncherStore.LoadAll();
        var entry = Assert.Single(entries);
        Assert.Equal("Dev", entry.Category);
        Assert.Equal("--first", entry.Arguments);
        Assert.Equal("First", entry.DisplayName);
    }

    [Fact]
    public void Add_WhitespacePath_DoesNothing()
    {
        using var scope = new StoreIsolationScope();

        LauncherStore.Add("   ");

        var entries = LauncherStore.LoadAll();
        Assert.Empty(entries);
    }

    [Fact]
    public void LoadAll_InvalidJson_FallsBackToLegacyEntries()
    {
        using var scope = new StoreIsolationScope();

        File.WriteAllText(scope.StoreFilePath, "{ invalid json }");
        File.WriteAllLines(scope.LegacyStoreFilePath,
        [
            "  C:\\Tools\\LegacyA.exe  ",
            "",
            "C:\\Tools\\LegacyB.exe"
        ]);

        var entries = LauncherStore.LoadAll().OrderBy(x => x.Path, StringComparer.OrdinalIgnoreCase).ToList();

        Assert.Equal(2, entries.Count);
        Assert.Contains(entries, e => e.Path.Equals(Path.GetFullPath(@"C:\Tools\LegacyA.exe"), StringComparison.OrdinalIgnoreCase));
        Assert.Contains(entries, e => e.Path.Equals(Path.GetFullPath(@"C:\Tools\LegacyB.exe"), StringComparison.OrdinalIgnoreCase));

        // Legacy load should also persist normalized JSON for next load.
        Assert.True(File.Exists(scope.StoreFilePath));
    }

    [Fact]
    public void SaveAll_NormalizesAndRemovesDuplicates()
    {
        using var scope = new StoreIsolationScope();

        var input = new[]
        {
            new LauncherStore.LauncherEntry("  C:\\Apps\\Tool.exe  ", "  Dev  ", "  -a  ", "  Tool Name  "),
            new LauncherStore.LauncherEntry("c:\\apps\\tool.exe", "Ops", "-b", "Other Name"),
            new LauncherStore.LauncherEntry("", "Ops", "-x", "X")
        };

        LauncherStore.SaveAll(input);

        var entries = LauncherStore.LoadAll();
        var entry = Assert.Single(entries);
        Assert.Equal(Path.GetFullPath(@"C:\Apps\Tool.exe"), entry.Path);
        Assert.Equal("Dev", entry.Category);
        Assert.Equal("-a", entry.Arguments);
        Assert.Equal("Tool Name", entry.DisplayName);
    }

    private sealed class StoreIsolationScope : IDisposable
    {
        private readonly string _storeDirectory;
        private readonly string? _backupDirectory;

        public string StoreFilePath { get; }
        public string LegacyStoreFilePath { get; }

        public StoreIsolationScope()
        {
            _storeDirectory = GetPrivateStaticStringField("StoreDirectory");
            StoreFilePath = GetPrivateStaticStringField("StoreFilePath");
            LegacyStoreFilePath = GetPrivateStaticStringField("LegacyStoreFilePath");

            if (Directory.Exists(_storeDirectory))
            {
                _backupDirectory = _storeDirectory + ".backup." + Guid.NewGuid().ToString("N");
                Directory.Move(_storeDirectory, _backupDirectory);
            }

            Directory.CreateDirectory(_storeDirectory);
        }

        public void Dispose()
        {
            try
            {
                if (Directory.Exists(_storeDirectory))
                {
                    Directory.Delete(_storeDirectory, recursive: true);
                }
            }
            finally
            {
                if (!string.IsNullOrEmpty(_backupDirectory) && Directory.Exists(_backupDirectory))
                {
                    Directory.Move(_backupDirectory, _storeDirectory);
                }
            }
        }

        private static string GetPrivateStaticStringField(string fieldName)
        {
            var field = typeof(LauncherStore).GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Static);
            Assert.NotNull(field);
            return (string)field!.GetValue(null)!;
        }
    }
}

[CollectionDefinition(nameof(LauncherStoreIoCollection), DisableParallelization = true)]
public class LauncherStoreIoCollection
{
}
