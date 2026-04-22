namespace applanch.ThemeCreator;

internal sealed class ThemeCreatorBaseThemeChoice
{
    internal ThemeCreatorBaseThemeChoice(string? id, string label)
    {
        Id = id;
        Label = label;
    }

    public string? Id { get; }

    public string Label { get; }

    public override string ToString() => Label;
}