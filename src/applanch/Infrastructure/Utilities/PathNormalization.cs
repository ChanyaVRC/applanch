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

    internal static bool IsUrl(string path)
    {
        return Uri.TryCreate(path, UriKind.Absolute, out var uri)
            && uri.Scheme != Uri.UriSchemeFile
            && path.Length > uri.Scheme.Length + 1;
    }

    internal static bool TryNormalizePersistablePath(string path, out string normalizedPath)
    {
        var candidatePath = NormalizeDriveSpecifier(path.Trim());
        if (string.IsNullOrWhiteSpace(candidatePath))
        {
            normalizedPath = string.Empty;
            return false;
        }

        if (IsUrl(candidatePath))
        {
            normalizedPath = candidatePath;
            return true;
        }

        if (!Path.IsPathFullyQualified(candidatePath))
        {
            normalizedPath = string.Empty;
            return false;
        }

        normalizedPath = NormalizePersistablePathCore(candidatePath);
        return true;
    }

    private static string NormalizePersistablePathCore(string path)
    {
        try
        {
            var full = Path.GetFullPath(path);
            return Directory.Exists(full)
                ? EnsureTrailingDirectorySeparator(full)
                : Path.TrimEndingDirectorySeparator(full);
        }
        catch (Exception ex)
        {
            AppLogger.Instance.Warn($"Path normalization failed for '{path}': {ex.Message}");
            return path;
        }
    }

    private static bool IsDriveLetterSpecifier(ReadOnlySpan<char> path) =>
        path.Length == 2 && char.IsLetter(path[0]) && path[1] == ':';

    private static string NormalizeDriveSpecifier(string path) =>
        IsDriveLetterSpecifier(path.AsSpan())
            ? path + Path.DirectorySeparatorChar
            : path;

    private static string EnsureTrailingDirectorySeparator(string path) =>
        Path.EndsInDirectorySeparator(path)
            ? path
            : path + Path.DirectorySeparatorChar;
}