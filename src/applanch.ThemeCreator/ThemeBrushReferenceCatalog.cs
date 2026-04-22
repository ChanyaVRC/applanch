using applanch.Theming;

namespace applanch.ThemeCreator;

internal static class ThemeBrushReferenceCatalog
{
    private static readonly Dictionary<string, ThemeBrushReference> References = Enum.GetValues<ThemeBrushKey>()
        .ToDictionary(static key => key.ToResourceKey(), static key => new ThemeBrushReference(key.ToResourceKey()), StringComparer.OrdinalIgnoreCase);

    internal static ThemeBrushReference[] HighlightedReferences { get; } =
    [
        References[ThemeBrushKey.AppBackground.ToResourceKey()],
        References[ThemeBrushKey.Surface.ToResourceKey()],
        References[ThemeBrushKey.TextPrimary.ToResourceKey()],
        References[ThemeBrushKey.TextSecondary.ToResourceKey()],
        References[ThemeBrushKey.ItemBackground.ToResourceKey()],
        References[ThemeBrushKey.ItemBorder.ToResourceKey()],
        References[ThemeBrushKey.NotificationWarningBorder.ToResourceKey()],
        References[ThemeBrushKey.DialogError.ToResourceKey()],
    ];

    internal static IReadOnlyList<string> Keys { get; } = References.Keys
        .OrderBy(static key => key, StringComparer.OrdinalIgnoreCase)
        .ToArray();

    internal static string GetDescription(string key)
    {
        if (References.TryGetValue(key, out var reference))
        {
            return reference.Description;
        }

        return ThemeCreatorResourceCatalog.GetBrushDescription(key);
    }
}
