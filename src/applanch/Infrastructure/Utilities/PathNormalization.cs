using System.IO;

namespace applanch.Infrastructure.Utilities;

internal static class PathNormalization
{
    internal static string NormalizeForComparison(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return string.Empty;
        }

        try
        {
            var fullPath = Path.GetFullPath(path.Trim());
            return Path.TrimEndingDirectorySeparator(fullPath);
        }
        catch (Exception)
        {
            return path.Trim();
        }
    }

    internal static string NormalizeDirectoryPath(string path)
    {
        if (IsDriveLetterSpecifier(path))
        {
            return path + Path.DirectorySeparatorChar;
        }

        return Path.TrimEndingDirectorySeparator(path);
    }

    internal static bool TryNormalizePersistablePath(string path, out string normalizedPath)
    {
        normalizedPath = string.Empty;

        var trimmedPathSpan = path.AsSpan().Trim();
        if (trimmedPathSpan.IsEmpty)
        {
            return false;
        }

        ReadOnlySpan<char> candidatePath = trimmedPathSpan;
        string? driveRootPath = null;
        if (IsDriveLetterSpecifier(trimmedPathSpan))
        {
            driveRootPath = string.Concat(trimmedPathSpan.ToString(), Path.DirectorySeparatorChar);
            candidatePath = driveRootPath.AsSpan();
        }

        if (!Path.IsPathFullyQualified(candidatePath))
        {
            return false;
        }

        normalizedPath = NormalizePersistablePathCore(candidatePath);
        return !string.IsNullOrWhiteSpace(normalizedPath);
    }

    private static string NormalizePersistablePathCore(ReadOnlySpan<char> path)
    {
        var trimmedPathSpan = path.Trim();
        if (trimmedPathSpan.IsEmpty)
        {
            return string.Empty;
        }

        var trimmedPath = trimmedPathSpan.ToString();
        try
        {
            var full = Path.GetFullPath(trimmedPath);
            return Directory.Exists(full)
                ? EnsureTrailingDirectorySeparator(full)
                : Path.TrimEndingDirectorySeparator(full);
        }
        catch (Exception ex)
        {
            AppLogger.Instance.Warn($"Path normalization failed for '{trimmedPath}': {ex.Message}");
            return trimmedPath;
        }
    }

    private static bool IsDriveLetterSpecifier(ReadOnlySpan<char> path) =>
        path.Length == 2 && char.IsLetter(path[0]) && path[1] == ':';

    private static bool IsDriveLetterSpecifier(string path)
    {
        var trimmed = path.AsSpan().Trim();
        return IsDriveLetterSpecifier(trimmed);
    }

    private static string EnsureTrailingDirectorySeparator(string path) =>
        Path.EndsInDirectorySeparator(path)
            ? path
            : path + Path.DirectorySeparatorChar;
}