using System.Diagnostics.CodeAnalysis;
using Microsoft.Win32;
using System.IO;
using System.Text;

namespace applanch.Infrastructure.Resolution;

internal static partial class AppResolver
{
    private static readonly WindowsAppResolverPlatform Platform = new();

    private static readonly Dictionary<string, string[]> KnownAliases = new(StringComparer.OrdinalIgnoreCase)
    {
        ["vscode"] = ["code", "code.exe"],
        ["visual studio code"] = ["code", "code.exe"],
        ["code"] = ["code", "code.exe"],
        ["notepad"] = ["notepad.exe"],
        ["powershell"] = ["powershell.exe", "pwsh.exe"],
        ["cmd"] = ["cmd.exe"]
    };

    private static readonly Lazy<IReadOnlyList<ResolvedApp>> InstalledAppsCache =
        new(() => Platform.LoadInstalledApps(), isThreadSafe: true);

    private static readonly Lazy<IReadOnlyList<string>> InstalledAppDisplayNamesCache =
        new(() =>
            [.. InstalledAppsCache.Value
                .Select(static app => app.DisplayName)
                .Distinct(StringComparer.OrdinalIgnoreCase)],
            isThreadSafe: true);

    public static bool TryResolve(string input, out ResolvedApp resolvedApp)
    {
        resolvedApp = default;

        if (string.IsNullOrWhiteSpace(input))
        {
            return false;
        }

        var trimmed = input.Trim();

        if (File.Exists(trimmed))
        {
            resolvedApp = new ResolvedApp(trimmed, Path.GetFileNameWithoutExtension(trimmed));
            return true;
        }

        if (Directory.Exists(trimmed))
        {
            var normalizedDirectoryPath = NormalizeDirectoryPath(trimmed);
            resolvedApp = new ResolvedApp(normalizedDirectoryPath, Path.GetFileName(normalizedDirectoryPath));
            return true;
        }

        foreach (var candidate in ExpandCandidates(trimmed))
        {
            if (Platform.TryResolveFromAppPaths(candidate, out resolvedApp) ||
                Platform.TryResolveFromPath(candidate, out resolvedApp))
            {
                return true;
            }
        }

        if (TryResolveFromInstalledPrograms(trimmed, out resolvedApp))
        {
            return true;
        }

        return false;
    }

    public static IReadOnlyList<string> GetSuggestions(string input, int maxResults = 8)
    {
        if (maxResults <= 0)
        {
            return [];
        }

        var trimmed = input?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(trimmed))
        {
            return [.. InstalledAppDisplayNamesCache.Value.Take(maxResults)];
        }

        var installedApps = InstalledAppsCache.Value;
        var candidates = new List<SuggestionCandidate>(installedApps.Count + KnownAliases.Count + maxResults);

        foreach (var app in installedApps)
        {
            var score = ScoreDisplayName(app.DisplayName, trimmed);
            if (score <= 0)
            {
                continue;
            }

            candidates.Add(new(app.DisplayName, score, 2));
        }

        foreach (var alias in KnownAliases.Keys)
        {
            var score = ScoreDisplayName(alias, trimmed);
            if (score <= 0)
            {
                continue;
            }

            candidates.Add(new(alias, score, 1));
        }

        foreach (var pathSuggestion in GetPathSuggestions(trimmed, maxResults))
        {
            candidates.Add(pathSuggestion);
        }

        return [.. candidates
            .OrderByDescending(static x => x.Score)
            .ThenByDescending(static x => x.SourcePriority)
            .ThenBy(static x => x.Text, StringComparer.CurrentCultureIgnoreCase)
            .DistinctBy(static x => x.Text, StringComparer.OrdinalIgnoreCase)
            .Take(maxResults)
            .Select(static x => x.Text)];
    }

    private static IEnumerable<string> ExpandCandidates(string input)
    {
        if (KnownAliases.TryGetValue(input, out var aliases))
        {
            foreach (var alias in aliases)
            {
                yield return alias;
            }
        }

        yield return input;

        if (!Path.HasExtension(input))
        {
            yield return $"{input}.exe";
        }
    }

    private static bool TryResolveFromInstalledPrograms(string input, out ResolvedApp resolvedApp)
    {
        var bestScore = 0;
        ResolvedApp best = default;
        foreach (var app in InstalledAppsCache.Value)
        {
            var score = ScoreDisplayName(app.DisplayName, input);
            if (score <= 0)
            {
                continue;
            }

            if (score > bestScore)
            {
                bestScore = score;
                best = app;
                continue;
            }

            if (score == bestScore && !string.IsNullOrWhiteSpace(best.DisplayName) &&
                string.Compare(app.DisplayName, best.DisplayName, StringComparison.CurrentCultureIgnoreCase) < 0)
            {
                best = app;
            }
        }

        if (!string.IsNullOrWhiteSpace(best.Path))
        {
            resolvedApp = best;
            return true;
        }

        resolvedApp = default;
        return false;
    }

    private static IEnumerable<SuggestionCandidate> GetPathSuggestions(string input, int maxResults)
    {
        if (!LooksLikePath(input))
        {
            yield break;
        }

        string directory;
        string prefix;

        if (Directory.Exists(input))
        {
            directory = input;
            prefix = string.Empty;
        }
        else
        {
            directory = Path.GetDirectoryName(input) ?? string.Empty;
            prefix = Path.GetFileName(input);
        }

        if (string.IsNullOrWhiteSpace(directory) || !Directory.Exists(directory))
        {
            yield break;
        }

        if (!Platform.TryEnumerateFileSystemEntries(directory, out var entries))
        {
            yield break;
        }

        var count = 0;
        foreach (var entry in entries)
        {
            var name = Path.GetFileName(entry);
            if (string.IsNullOrEmpty(name))
            {
                continue;
            }

            var score = ScoreDisplayName(name, prefix);
            if (score <= 0)
            {
                continue;
            }

            yield return new(entry, score + 10, 0);

            count++;
            if (count >= maxResults)
            {
                yield break;
            }
        }
    }

    private static bool LooksLikePath(string input) => input.IndexOfAny(['\\', '/', ':']) >= 0;

    private static string NormalizeDirectoryPath(string path) =>
        path.Length <= 3 ? path : path.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);

    private static int ScoreDisplayName(string displayName, string input)
    {
        if (string.IsNullOrWhiteSpace(input))
        {
            return 0;
        }

        if (string.Equals(displayName, input, StringComparison.OrdinalIgnoreCase))
        {
            return 100;
        }

        if (displayName.StartsWith(input, StringComparison.OrdinalIgnoreCase))
        {
            return 80;
        }

        return displayName.Contains(input, StringComparison.OrdinalIgnoreCase) ? 50 : 0;
    }

    private static bool TryGetFirstExecutableInDirectory(string directory, [NotNullWhen(true)] out string? executablePath)
    {
        executablePath = null;

        try
        {
            executablePath = Directory.EnumerateFiles(directory, "*.exe", SearchOption.TopDirectoryOnly).FirstOrDefault();
        }
        catch (UnauthorizedAccessException)
        {
            return false;
        }
        catch (IOException)
        {
            return false;
        }

        return !string.IsNullOrWhiteSpace(executablePath) && File.Exists(executablePath);
    }

    private static bool TryExtractExecutablePath(RegistryKey appKey, [NotNullWhen(true)] out string? path)
    {
        path = null;

        if (appKey.GetValue("DisplayIcon") is string displayIcon && TryParseExecutablePath(displayIcon, out path))
        {
            return true;
        }

        if (appKey.GetValue("InstallLocation") is string installLocation &&
            !string.IsNullOrWhiteSpace(installLocation) &&
            Directory.Exists(installLocation) &&
            TryGetFirstExecutableInDirectory(installLocation, out var candidate))
        {
            path = candidate;
            return true;
        }

        return false;
    }

    private static bool TryParseExecutablePath(string? raw, [NotNullWhen(true)] out string? path)
    {
        path = null;
        if (string.IsNullOrWhiteSpace(raw))
        {
            return false;
        }

        var cleaned = ExtractExecutablePathCandidate(raw);
        if (!cleaned.EndsWith(".exe", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        if (!File.Exists(cleaned))
        {
            return false;
        }

        path = cleaned;
        return true;
    }

    private static string ExtractExecutablePathCandidate(string raw)
    {
        var cleaned = raw.Trim();

        if (cleaned.StartsWith('"'))
        {
            var closingQuoteIndex = cleaned.IndexOf('"', 1);
            if (closingQuoteIndex > 1)
            {
                return cleaned[1..closingQuoteIndex].Trim();
            }
        }

        var exeIndex = FindExecutableExtensionIndex(cleaned);
        if (exeIndex >= 0)
        {
            return cleaned[..(exeIndex + 4)].Trim().Trim('"');
        }

        var commaIndex = IndexOfFirstUnquoted(cleaned, ',');
        if (commaIndex >= 0)
        {
            cleaned = cleaned[..commaIndex];
        }

        var firstWhitespace = cleaned.IndexOfAny([' ', '\t']);
        if (firstWhitespace > 0)
        {
            cleaned = cleaned[..firstWhitespace];
        }

        return cleaned.Trim().Trim('"');
    }

    private static int IndexOfFirstUnquoted(string text, char target)
    {
        var inQuotes = false;

        for (var i = 0; i < text.Length; i++)
        {
            var ch = text[i];
            if (ch == '"')
            {
                inQuotes = !inQuotes;
                continue;
            }

            if (!inQuotes && ch == target)
            {
                return i;
            }
        }

        return -1;
    }

    private static int FindExecutableExtensionIndex(string value)
    {
        var searchStart = 0;
        while (searchStart < value.Length)
        {
            var index = value.IndexOf(".exe", searchStart, StringComparison.OrdinalIgnoreCase);
            if (index < 0)
            {
                return -1;
            }

            var nextIndex = index + 4;
            if (nextIndex >= value.Length)
            {
                return index;
            }

            var nextChar = value[nextIndex];
            if (nextChar == ',' || char.IsWhiteSpace(nextChar) || nextChar == '"')
            {
                return index;
            }

            searchStart = index + 1;
        }

        return -1;
    }
}

