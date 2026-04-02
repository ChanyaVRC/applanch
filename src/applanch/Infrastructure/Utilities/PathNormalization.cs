using System.Collections.Concurrent;
using System.IO;
using Microsoft.Win32;

namespace applanch.Infrastructure.Utilities;

internal static class PathNormalization
{
    private static readonly ConcurrentDictionary<string, bool> RegisteredUriSchemeCache =
        new(StringComparer.OrdinalIgnoreCase);

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

    internal static PathType GetPathType(string path)
    {
        return GetPathType(path, out _);
    }

    internal static PathType GetPathType(string path, out Uri? uri)
    {
        uri = null;
        if (!TryParseRegisteredUrl(path, out var parsedUri))
        {
            return PathType.FileSystem;
        }

        uri = parsedUri;
        return IsHttpScheme(parsedUri.Scheme)
            ? PathType.HttpUrl
            : PathType.RegisteredUrl;
    }

    internal static bool IsUrl(string path)
    {
        return GetPathType(path) is not PathType.FileSystem;
    }

    internal static bool IsHttpUrl(string path)
    {
        return GetPathType(path) is PathType.HttpUrl;
    }

    internal static bool TryParseHttpUrl(string path, out Uri uri)
    {
        if (GetPathType(path, out var parsedUri) is PathType.HttpUrl && parsedUri is not null)
        {
            uri = parsedUri;
            return true;
        }

        uri = default!;
        return false;
    }

    internal static bool TryParseRegisteredUrl(string path, out Uri uri)
    {
        uri = default!;

        if (!Uri.TryCreate(path, UriKind.Absolute, out var parsedUri) || parsedUri.Scheme == Uri.UriSchemeFile)
        {
            return false;
        }

        uri = parsedUri;

        // Require "://" to distinguish proper URL schemes from Windows device names (e.g. "CON:something").
        var s = uri.Scheme.Length;
        if (path.Length <= s + 2 || path[s] != ':' || path[s + 1] != '/' || path[s + 2] != '/')
        {
            return false;
        }

        return IsRegisteredUriScheme(uri.Scheme);
    }

    private static bool IsHttpScheme(string scheme)
    {
        return string.Equals(scheme, Uri.UriSchemeHttp, StringComparison.OrdinalIgnoreCase) ||
               string.Equals(scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsRegisteredUriScheme(string scheme)
    {
        return RegisteredUriSchemeCache.GetOrAdd(scheme, static s =>
        {
            using var key = Registry.ClassesRoot.OpenSubKey(s);
            return key?.GetValue("URL Protocol") != null;
        });
    }

    internal static bool TryNormalizePersistablePath(string path, out string normalizedPath)
    {
        var candidatePath = NormalizeDriveSpecifier(path.Trim());
        if (string.IsNullOrWhiteSpace(candidatePath))
        {
            normalizedPath = string.Empty;
            return false;
        }

        if (GetPathType(candidatePath) is not PathType.FileSystem)
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

    internal static string NormalizeLaunchPath(string path)
    {
        var trimmed = path.Trim();
        var normalizedPath = TryNormalizePersistablePath(trimmed, out var persistablePath)
            ? persistablePath
            : NormalizeForComparison(trimmed);

        return GetPathType(normalizedPath) is PathType.FileSystem
            ? NormalizeForComparison(normalizedPath)
            : normalizedPath;
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
