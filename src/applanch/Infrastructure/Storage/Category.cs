namespace applanch.Infrastructure.Storage;

public readonly record struct Category
{
    public static Category Default => new(LauncherEntry.DefaultCategory);

    public string Value { get; }

    public bool IsDefault => string.IsNullOrEmpty(Value);

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

        return Value;
    }

    public override string ToString() => Value;
}
