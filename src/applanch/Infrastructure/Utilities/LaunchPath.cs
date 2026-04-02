namespace applanch.Infrastructure.Utilities;

public readonly struct LaunchPath : IEquatable<LaunchPath>
{
    public string Value { get; }

    public PathType Type { get; }

    public Uri? ParsedUri { get; }

    public bool IsUrl => Type is not PathType.FileSystem;

    public bool IsHttpUrl => Type is PathType.HttpUrl;

    public LaunchPath(string fullPath)
    {
        if (string.IsNullOrWhiteSpace(fullPath))
        {
            throw new ArgumentException("Launch path must not be empty.", nameof(fullPath));
        }

        var trimmed = fullPath.Trim();
        var normalizedValue = PathNormalization.TryNormalizePersistablePath(trimmed, out var normalizedPath)
            ? normalizedPath
            : PathNormalization.NormalizeForComparison(trimmed);

        var pathType = PathNormalization.GetPathType(normalizedValue, out var parsedUri);
        if (pathType is PathType.FileSystem)
        {
            normalizedValue = PathNormalization.NormalizeForComparison(normalizedValue);
        }

        Value = normalizedValue;
        Type = pathType;
        ParsedUri = parsedUri;
    }

    public static bool TryCreate(string fullPath, out LaunchPath launchPath)
    {
        if (string.IsNullOrWhiteSpace(fullPath))
        {
            launchPath = default;
            return false;
        }

        launchPath = new LaunchPath(fullPath);
        return true;
    }

    public override string ToString() => Value;

    public bool Equals(LaunchPath other) =>
        string.Equals(Value, other.Value, StringComparison.OrdinalIgnoreCase);

    public override bool Equals(object? obj) => obj is LaunchPath other && Equals(other);

    public override int GetHashCode() => StringComparer.OrdinalIgnoreCase.GetHashCode(Value ?? string.Empty);

    public static bool operator ==(LaunchPath left, LaunchPath right) => left.Equals(right);

    public static bool operator !=(LaunchPath left, LaunchPath right) => !left.Equals(right);
}
