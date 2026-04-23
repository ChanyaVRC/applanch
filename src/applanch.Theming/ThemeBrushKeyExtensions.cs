namespace applanch.Theming;

public static class ThemeBrushKeyExtensions
{
    private const string BrushPrefix = "Brush.";

    public static string ToResourceKey(this ThemeBrushKey key) => $"{BrushPrefix}{key}";

    public static bool TryParseResourceKey(string? resourceKey, out ThemeBrushKey key)
    {
        if (string.IsNullOrWhiteSpace(resourceKey) ||
            !resourceKey.StartsWith(BrushPrefix, StringComparison.OrdinalIgnoreCase))
        {
            key = default;
            return false;
        }

        var keyName = resourceKey[BrushPrefix.Length..];
        return Enum.TryParse(keyName, ignoreCase: true, out key) && Enum.IsDefined(key);
    }
}