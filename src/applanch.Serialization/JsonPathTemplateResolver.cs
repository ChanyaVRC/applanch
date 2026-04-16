namespace applanch.Serialization;

public static class JsonPathTemplateResolver
{
    public static string ResolveExistingFilePath(string templatePath, string basePath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(templatePath);
        ArgumentException.ThrowIfNullOrWhiteSpace(basePath);

        var expandedTemplate = Environment.ExpandEnvironmentVariables(templatePath);
        if (string.IsNullOrWhiteSpace(expandedTemplate))
        {
            throw new JsonPathResolutionException("Path template resolved to empty value.");
        }

        var candidatePath = ResolveTemplatePath(expandedTemplate, basePath);
        if (string.IsNullOrWhiteSpace(candidatePath))
        {
            throw new JsonPathResolutionException("Could not resolve base directory for relative path template.");
        }

        try
        {
            if (ContainsWildcard(candidatePath))
            {
                var resolvedWildcardPath = ResolveWildcardPath(candidatePath);
                if (!string.IsNullOrWhiteSpace(resolvedWildcardPath))
                {
                    return resolvedWildcardPath;
                }

                throw new JsonPathResolutionException($"Wildcard path did not match any file: {candidatePath}");
            }

            var fullPath = Path.GetFullPath(candidatePath);
            if (File.Exists(fullPath))
            {
                return fullPath;
            }

            throw new JsonPathResolutionException($"Path does not exist: {fullPath}");
        }
        catch (JsonPathResolutionException)
        {
            throw;
        }
        catch (Exception ex)
        {
            throw new JsonPathResolutionException($"File system error while resolving path '{candidatePath}': {ex.Message}", ex);
        }
    }

    private static string? ResolveTemplatePath(string templatePath, string basePath)
    {
        if (Path.IsPathRooted(templatePath))
        {
            return templatePath;
        }

        var baseDirectory = Path.GetDirectoryName(basePath);
        if (string.IsNullOrWhiteSpace(baseDirectory))
        {
            return null;
        }

        return Path.Combine(baseDirectory, templatePath);
    }

    private static string? ResolveWildcardPath(string wildcardPath)
    {
        var root = Path.GetPathRoot(wildcardPath);
        if (string.IsNullOrWhiteSpace(root))
        {
            return null;
        }

        var relative = wildcardPath[root.Length..].TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        var segments = relative.Split(['\\', '/'], StringSplitOptions.RemoveEmptyEntries);
        if (segments.Length == 0)
        {
            return null;
        }

        var directories = new List<string> { Path.TrimEndingDirectorySeparator(root) };

        for (var i = 0; i < segments.Length - 1; i++)
        {
            var segment = segments[i];
            var nextDirectories = new List<string>();
            foreach (var directory in directories)
            {
                if (!Directory.Exists(directory))
                {
                    continue;
                }

                if (ContainsWildcard(segment))
                {
                    nextDirectories.AddRange(Directory.EnumerateDirectories(directory, segment, SearchOption.TopDirectoryOnly));
                    continue;
                }

                var fixedDirectory = Path.Combine(directory, segment);
                if (Directory.Exists(fixedDirectory))
                {
                    nextDirectories.Add(fixedDirectory);
                }
            }

            if (nextDirectories.Count == 0)
            {
                return null;
            }

            directories = nextDirectories;
        }

        var filePattern = segments[^1];
        var candidates = new List<string>();
        foreach (var directory in directories)
        {
            if (!Directory.Exists(directory))
            {
                continue;
            }

            if (ContainsWildcard(filePattern))
            {
                candidates.AddRange(Directory.EnumerateFiles(directory, filePattern, SearchOption.TopDirectoryOnly));
                continue;
            }

            var fixedFile = Path.Combine(directory, filePattern);
            if (File.Exists(fixedFile))
            {
                candidates.Add(fixedFile);
            }
        }

        return candidates
            .OrderByDescending(static path => File.GetLastWriteTimeUtc(path))
            .FirstOrDefault();
    }

    private static bool ContainsWildcard(string value)
    {
        return value.IndexOfAny(['*', '?']) >= 0;
    }
}
