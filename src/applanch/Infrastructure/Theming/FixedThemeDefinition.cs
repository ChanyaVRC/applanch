namespace applanch.Infrastructure.Theming;

internal sealed class FixedThemeDefinition(
    string id,
    LocalizedText displayName,
    string? inheritedThemeId = null) : ThemeDefinition(id, displayName)
{
    internal string? InheritedThemeId { get; } = inheritedThemeId;

    internal override bool TryGetInheritedThemeId(out string inheritedThemeId)
    {
        if (string.IsNullOrWhiteSpace(InheritedThemeId))
        {
            inheritedThemeId = string.Empty;
            return false;
        }

        inheritedThemeId = InheritedThemeId;
        return true;
    }
}
