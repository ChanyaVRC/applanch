namespace applanch.Infrastructure.Updates;

public readonly record struct SemanticVersion(int Major, int Minor, int Patch, string Prerelease) : IComparable<SemanticVersion>
{
    public bool IsPrerelease => !string.IsNullOrEmpty(Prerelease);

    public override string ToString()
    {
        return IsPrerelease
            ? $"{Major}.{Minor}.{Patch}-{Prerelease}"
            : $"{Major}.{Minor}.{Patch}";
    }

    public static SemanticVersion Parse(string input)
    {
        return Parse(input.AsSpan());
    }

    public static SemanticVersion Parse(ReadOnlySpan<char> input)
    {
        if (!TryParse(input, out var result))
            throw new FormatException($"'{input}' is not a valid semantic version.");
        return result;
    }

    public static bool TryParse(string input, out SemanticVersion result)
    {
        return TryParse(input.AsSpan(), out result);
    }

    public static bool TryParse(ReadOnlySpan<char> input, out SemanticVersion result)
    {
        if (input.IsEmpty)
        {
            result = default;
            return false;
        }

        var span = input.Trim();
        if (!span.IsEmpty && (span[0] == 'v' || span[0] == 'V'))
        {
            span = span[1..];
        }

        Span<Range> prereleaseParts = stackalloc Range[2];
        var prereleasePartCount = span.Split(prereleaseParts, '-');
        if (prereleasePartCount > 2)
        {
            result = default;
            return false;
        }

        var numericSpan = span[prereleaseParts[0]];

        Span<Range> segments = stackalloc Range[4];
        var segmentCount = numericSpan.Split(segments, '.');

        if (segmentCount != 3 ||
            !int.TryParse(numericSpan[segments[0]], out var major) ||
            !int.TryParse(numericSpan[segments[1]], out var minor) ||
            !int.TryParse(numericSpan[segments[2]], out var patch) ||
            major < 0 ||
            minor < 0 ||
            patch < 0)
        {
            result = default;
            return false;
        }

        var prerelease = string.Empty;
        if (prereleasePartCount == 2)
        {
            var prereleaseSpan = span[prereleaseParts[1]];
            if (prereleaseSpan.IsEmpty)
            {
                result = default;
                return false;
            }

            prerelease = prereleaseSpan.ToString();
        }

        result = new SemanticVersion(major, minor, patch, prerelease);
        return true;
    }

    public int CompareTo(SemanticVersion other)
    {
        var numericComparison = (Major, Minor, Patch, other.IsPrerelease).CompareTo((other.Major, other.Minor, other.Patch, IsPrerelease));
        if (numericComparison != 0)
        {
            return numericComparison;
        }

        return ComparePrerelease(Prerelease.AsSpan(), other.Prerelease.AsSpan());
    }

    private static int ComparePrerelease(ReadOnlySpan<char> left, ReadOnlySpan<char> right)
    {
        Span<Range> leftParts = stackalloc Range[16];
        Span<Range> rightParts = stackalloc Range[16];
        var leftCount = left.Split(leftParts, '.');
        var rightCount = right.Split(rightParts, '.');
        var count = Math.Min(leftCount, rightCount);

        for (var index = 0; index < count; index++)
        {
            var leftPart = left[leftParts[index]];
            var rightPart = right[rightParts[index]];

            var isLeftNumeric = int.TryParse(leftPart, out var leftNumeric);
            var isRightNumeric = int.TryParse(rightPart, out var rightNumeric);

            if (isLeftNumeric && isRightNumeric)
            {
                var numericComparison = leftNumeric.CompareTo(rightNumeric);
                if (numericComparison != 0)
                {
                    return numericComparison;
                }

                continue;
            }

            var isNumberComparison = isRightNumeric.CompareTo(isLeftNumeric);
            if (isNumberComparison != 0)
            {
                return isNumberComparison;
            }

            var lexicalComparison = leftPart.CompareTo(rightPart, StringComparison.Ordinal);
            if (lexicalComparison != 0)
            {
                return lexicalComparison;
            }
        }

        return leftCount.CompareTo(rightCount);
    }
}
