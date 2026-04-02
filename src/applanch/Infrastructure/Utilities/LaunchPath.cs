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
        Value = fullPath;
        Type = PathNormalization.GetPathType(fullPath, out var parsedUri);
        ParsedUri = parsedUri;
    }

    public override string ToString() => Value;

    public bool Equals(LaunchPath other) => Value == other.Value;

    public override bool Equals(object? obj) => obj is LaunchPath other && Equals(other);

    public override int GetHashCode() => StringComparer.OrdinalIgnoreCase.GetHashCode(Value ?? string.Empty);

    public static bool operator ==(LaunchPath left, LaunchPath right) => left.Equals(right);

    public static bool operator !=(LaunchPath left, LaunchPath right) => !left.Equals(right);
}
