namespace applanch.ThemeCreator;

internal sealed record ThemeBrushReference(string Key)
{
    public string Description => ThemeCreatorResourceCatalog.GetBrushDescription(Key);
}
