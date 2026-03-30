namespace applanch;

internal readonly record struct SemanticVersion(int Major, int Minor, int Patch, string Prerelease) : IComparable<SemanticVersion>
{
    public bool IsPrerelease => Prerelease.Length > 0;

    public static bool TryParse(string input, out SemanticVersion result)
    {
        result = default;

        var parts = input.Split('-', 2);
        var segments = parts[0].Split('.');

        if (segments.Length < 3 ||
            !int.TryParse(segments[0], out var major) ||
            !int.TryParse(segments[1], out var minor) ||
            !int.TryParse(segments[2], out var patch))
        {
            return false;
        }

        result = new SemanticVersion(major, minor, patch, parts.Length > 1 ? parts[1] : string.Empty);
        return true;
    }

    public int CompareTo(SemanticVersion other) =>
        (Major, Minor, Patch, other.IsPrerelease).CompareTo((other.Major, other.Minor, other.Patch, IsPrerelease));
}
