using System.Collections.Frozen;
using applanch.Theming;

namespace applanch.ThemeCreator;

internal static class ThemeBrushReferenceCatalog
{
    private static readonly FrozenDictionary<string, ThemeBrushReference> References = Enum.GetValues<ThemeBrushKey>()
        .ToFrozenDictionary(static key => key.ToResourceKey(), static key => new ThemeBrushReference(key.ToResourceKey()), StringComparer.OrdinalIgnoreCase);

    internal static IReadOnlyList<string> Keys { get; } = References.Keys;

    internal static string GetDescription(string key)
    {
        if (References.TryGetValue(key, out var reference))
        {
            return reference.Description;
        }

        return ThemeCreatorResourceCatalog.GetBrushDescription(key);
    }
}
