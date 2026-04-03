namespace applanch.Infrastructure.Updates;

internal readonly record struct SemanticVersion(int Major, int Minor, int Patch, string Prerelease) : IComparable<SemanticVersion>
{
    public bool IsPrerelease => !string.IsNullOrEmpty(Prerelease);

    public static bool TryParse(string input, out SemanticVersion result)
    {
        result = default;

        var span = input.AsSpan();
        var prereleaseSeparatorIndex = span.IndexOf('-');
        var numericSpan = prereleaseSeparatorIndex >= 0 ? span[..prereleaseSeparatorIndex] : span;

        if (!TryReadNthDotSegment(numericSpan, 0, out var majorSegment) ||
            !TryReadNthDotSegment(numericSpan, 1, out var minorSegment) ||
            !TryReadNthDotSegment(numericSpan, 2, out var patchSegment) ||
            !int.TryParse(majorSegment, out var major) ||
            !int.TryParse(minorSegment, out var minor) ||
            !int.TryParse(patchSegment, out var patch))
        {
            return false;
        }

        var prerelease = prereleaseSeparatorIndex >= 0 ? input[(prereleaseSeparatorIndex + 1)..] : string.Empty;
        result = new SemanticVersion(major, minor, patch, prerelease);
        return true;
    }

    private static bool TryReadNthDotSegment(ReadOnlySpan<char> text, int segmentIndex, out ReadOnlySpan<char> segment)
    {
        segment = text;
        var currentIndex = 0;

        while (currentIndex < segmentIndex)
        {
            var separatorIndex = segment.IndexOf('.');
            if (separatorIndex < 0)
            {
                segment = default;
                return false;
            }

            segment = segment[(separatorIndex + 1)..];
            currentIndex++;
        }

        var nextSeparatorIndex = segment.IndexOf('.');
        if (nextSeparatorIndex >= 0)
        {
            segment = segment[..nextSeparatorIndex];
        }

        return true;
    }

    public int CompareTo(SemanticVersion other) =>
        (Major, Minor, Patch, other.IsPrerelease).CompareTo((other.Major, other.Minor, other.Patch, IsPrerelease));
}

