using System.Collections.Immutable;

namespace applanch.ResourceGenerator;

internal sealed class ParsedResxFile
{
    public ParsedResxFile(string path, ImmutableArray<ResourceEntry> entries)
    {
        Path = path;
        Entries = entries;
    }

    public string Path { get; }

    public ImmutableArray<ResourceEntry> Entries { get; }
}
