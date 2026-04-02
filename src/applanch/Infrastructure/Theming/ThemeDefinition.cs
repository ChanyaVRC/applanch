namespace applanch.Infrastructure.Theming;

internal abstract class ThemeDefinition
{
    protected ThemeDefinition(string id, LocalizedText displayName)
    {
        Id = id;
        DisplayName = displayName;
    }

    internal string Id { get; }

    internal LocalizedText DisplayName { get; }

    internal virtual bool TryGetInheritedThemeId(out string inheritedThemeId)
    {
        inheritedThemeId = string.Empty;
        return false;
    }
}
