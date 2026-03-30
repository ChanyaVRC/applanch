using System.Collections.Generic;
using System.Collections.Immutable;
using System.Xml.Linq;

namespace applanch.ResourceGenerator;

internal static class ResxParser
{
    public static ParsedResxFile Parse(ResxFile file)
    {
        if (string.IsNullOrWhiteSpace(file.Content))
        {
            return new ParsedResxFile(file.Path, ImmutableArray<ResourceEntry>.Empty);
        }

        var doc = XDocument.Parse(file.Content, LoadOptions.None);
        var root = doc.Root;
        if (root is null)
        {
            return new ParsedResxFile(file.Path, ImmutableArray<ResourceEntry>.Empty);
        }

        var entries = new List<ResourceEntry>();
        var uniqueNames = new HashSet<string>(System.StringComparer.Ordinal);

        foreach (var element in root.Elements("data"))
        {
            var rawName = element.Attribute("name")?.Value;
            if (string.IsNullOrWhiteSpace(rawName))
            {
                continue;
            }

            var name = rawName!;

            if (uniqueNames.Add(name))
            {
                entries.Add(new ResourceEntry(name));
            }
        }

        return new ParsedResxFile(file.Path, entries.ToImmutableArray());
    }
}
