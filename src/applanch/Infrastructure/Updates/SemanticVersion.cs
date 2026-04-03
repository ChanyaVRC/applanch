namespace applanch.Infrastructure.Updates;

internal readonly record struct SemanticVersion(int Major, int Minor, int Patch, string Prerelease) : IComparable<SemanticVersion>
{
    public bool IsPrerelease => !string.IsNullOrEmpty(Prerelease);

    public static bool TryParse(string input, out SemanticVersion result)
    {
        result = default;

        var span = input.AsSpan();

        Span<Range> prereleaseParts = stackalloc Range[2];
        var prereleasePartCount = span.Split(prereleaseParts, '-');
        var numericSpan = span[prereleaseParts[0]];

        Span<Range> segments = stackalloc Range[4];
        var segmentCount = numericSpan.Split(segments, '.');

        if (segmentCount < 3 ||
            !int.TryParse(numericSpan[segments[0]], out var major) ||
            !int.TryParse(numericSpan[segments[1]], out var minor) ||
            !int.TryParse(numericSpan[segments[2]], out var patch))
        {
            return false;
        }

        var prerelease = prereleasePartCount > 1 ? span[prereleaseParts[1]].ToString() : string.Empty;
        result = new SemanticVersion(major, minor, patch, prerelease);
        return true;
    }

    public int CompareTo(SemanticVersion other) =>
        (Major, Minor, Patch, other.IsPrerelease).CompareTo((other.Major, other.Minor, other.Patch, IsPrerelease));
}

