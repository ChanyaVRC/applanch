namespace applanch.ThemeCreator;

internal static class ThemeBrushReferenceCatalog
{
    private static readonly Dictionary<string, ThemeBrushReference> References = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Brush.AppBackground"] = new("Brush.AppBackground"),
        ["Brush.Surface"] = new("Brush.Surface"),
        ["Brush.SurfaceBorder"] = new("Brush.SurfaceBorder"),
        ["Brush.TextPrimary"] = new("Brush.TextPrimary"),
        ["Brush.SidebarPinSlash"] = new("Brush.SidebarPinSlash"),
        ["Brush.TextSecondary"] = new("Brush.TextSecondary"),
        ["Brush.TextTertiary"] = new("Brush.TextTertiary"),
        ["Brush.ScrollbarThumb"] = new("Brush.ScrollbarThumb"),
        ["Brush.PrereleaseBadgeBackground"] = new("Brush.PrereleaseBadgeBackground"),
        ["Brush.PrereleaseBadgeBorder"] = new("Brush.PrereleaseBadgeBorder"),
        ["Brush.PrereleaseBadgeText"] = new("Brush.PrereleaseBadgeText"),
        ["Brush.ItemBackground"] = new("Brush.ItemBackground"),
        ["Brush.ItemBorder"] = new("Brush.ItemBorder"),
        ["Brush.IconBackground"] = new("Brush.IconBackground"),
        ["Brush.NotificationInfoBackground"] = new("Brush.NotificationInfoBackground"),
        ["Brush.NotificationInfoBorder"] = new("Brush.NotificationInfoBorder"),
        ["Brush.NotificationActionHover"] = new("Brush.NotificationActionHover"),
        ["Brush.NotificationWarningBackground"] = new("Brush.NotificationWarningBackground"),
        ["Brush.NotificationWarningBorder"] = new("Brush.NotificationWarningBorder"),
        ["Brush.MissingPathWarningBadge"] = new("Brush.MissingPathWarningBadge"),
        ["Brush.NotificationErrorBackground"] = new("Brush.NotificationErrorBackground"),
        ["Brush.NotificationErrorBorder"] = new("Brush.NotificationErrorBorder"),
        ["Brush.NotificationProgressTrack"] = new("Brush.NotificationProgressTrack"),
        ["Brush.NotificationProgressValue"] = new("Brush.NotificationProgressValue"),
        ["Brush.QuickAddInfoText"] = new("Brush.QuickAddInfoText"),
        ["Brush.QuickAddWarningText"] = new("Brush.QuickAddWarningText"),
        ["Brush.DialogInfo"] = new("Brush.DialogInfo"),
        ["Brush.DialogQuestion"] = new("Brush.DialogQuestion"),
        ["Brush.DialogWarning"] = new("Brush.DialogWarning"),
        ["Brush.DialogError"] = new("Brush.DialogError"),
    };

    internal static ThemeBrushReference[] HighlightedReferences { get; } =
    [
        References["Brush.AppBackground"],
        References["Brush.Surface"],
        References["Brush.TextPrimary"],
        References["Brush.TextSecondary"],
        References["Brush.ItemBackground"],
        References["Brush.ItemBorder"],
        References["Brush.NotificationWarningBorder"],
        References["Brush.DialogError"],
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
