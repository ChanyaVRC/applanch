namespace applanch.Infrastructure.Storage;

public readonly record struct Category
{
    public static Category Default => new(LauncherEntry.DefaultCategory);
    public static Category All => new(LauncherEntry.AllCategories);

    public string Value { get; }

    public bool IsDefault => Value == LauncherEntry.DefaultCategory;

    public bool IsAll => Value == LauncherEntry.AllCategories;

    private Category(string value)
    {
        Value = value;
    }

    public static Category FromInput(string? rawCategory)
    {
        return new Category(LaunchItemNormalization.NormalizeCategory(rawCategory));
    }

    public string ToDisplayLabel()
    {
        if (IsDefault)
        {
            return AppResources.DefaultCategory;
        }
        if (IsAll)
        {
            return AppResources.AllCategories;
        }

        return Value;
    }

    public override string ToString() => ToDisplayLabel();
}
