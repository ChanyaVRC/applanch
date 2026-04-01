using System.Diagnostics.CodeAnalysis;
using Microsoft.Win32;
using System.IO;
using System.Security;
using applanch.Infrastructure.Utilities;

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

        if (PathNormalization.TryParseRegisteredUrl(trimmed, out var uri))
        {
            var displayName = string.IsNullOrWhiteSpace(uri.Host) ? trimmed : uri.Host;
            resolvedApp = new ResolvedApp(trimmed, displayName);
            return true;
        }

        if (File.Exists(trimmed))
        {
            resolvedApp = new ResolvedApp(trimmed, Path.GetFileNameWithoutExtension(trimmed));
            return true;
        }

        if (Directory.Exists(trimmed))
        {
            var normalizedDirectoryPath = NormalizeResolvedDirectoryPath(trimmed);
            resolvedApp = new ResolvedApp(normalizedDirectoryPath, GetDirectoryDisplayName(normalizedDirectoryPath));
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

        var bestByText = new Dictionary<string, SuggestionCandidate>(StringComparer.OrdinalIgnoreCase);
        foreach (var candidate in candidates)
        {
            if (!bestByText.TryGetValue(candidate.Text, out var existing) ||
                IsHigherRank(candidate, existing))
            {
                bestByText[candidate.Text] = candidate;
            }
        }

        return SelectTopSuggestions(bestByText.Values, maxResults);
    }

    private static bool IsHigherRank(in SuggestionCandidate left, in SuggestionCandidate right)
    {
        return CompareSuggestionRank(left, right) > 0;
    }

    private static int CompareSuggestionRank(in SuggestionCandidate left, in SuggestionCandidate right)
    {
        if (left.Score != right.Score)
        {
            return left.Score.CompareTo(right.Score);
        }

        if (left.SourcePriority != right.SourcePriority)
        {
            return left.SourcePriority.CompareTo(right.SourcePriority);
        }

        return -string.Compare(left.Text, right.Text, StringComparison.CurrentCultureIgnoreCase);
    }

    private static IReadOnlyList<string> SelectTopSuggestions(Dictionary<string, SuggestionCandidate>.ValueCollection candidates, int maxResults)
    {
        if (candidates.Count <= maxResults)
        {
            return [.. candidates
                .OrderByDescending(static x => x.Score)
                .ThenByDescending(static x => x.SourcePriority)
                .ThenBy(static x => x.Text, StringComparer.CurrentCultureIgnoreCase)
                .Select(static x => x.Text)];
        }

        var selected = new List<SuggestionCandidate>(maxResults);
        foreach (var candidate in candidates)
        {
            if (selected.Count < maxResults)
            {
                selected.Add(candidate);
                continue;
            }

            var lowestRankIndex = 0;
            for (var i = 1; i < selected.Count; i++)
            {
                if (CompareSuggestionRank(selected[i], selected[lowestRankIndex]) < 0)
                {
                    lowestRankIndex = i;
                }
            }

            if (CompareSuggestionRank(candidate, selected[lowestRankIndex]) > 0)
            {
                selected[lowestRankIndex] = candidate;
            }
        }

        return [.. selected
            .OrderByDescending(static x => x.Score)
            .ThenByDescending(static x => x.SourcePriority)
            .ThenBy(static x => x.Text, StringComparer.CurrentCultureIgnoreCase)
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
        if (!TryResolvePathSuggestionInput(input, out var directory, out var prefix))
        {
            yield break;
        }

        if (!Platform.TryEnumerateFileSystemEntries(directory, out var entries))
        {
            yield break;
        }

        var producedCount = 0;
        foreach (var entry in entries)
        {
            if (!TryCreatePathSuggestion(entry, prefix, out var suggestion))
            {
                continue;
            }

            yield return suggestion;

            producedCount++;
            if (producedCount >= maxResults)
            {
                yield break;
            }
        }
    }

    private static bool TryResolvePathSuggestionInput(string input, out string directory, out string prefix)
    {
        directory = string.Empty;
        prefix = string.Empty;

        if (!LooksLikePath(input))
        {
            return false;
        }

        if (Directory.Exists(input))
        {
            directory = input;
            return true;
        }

        directory = Path.GetDirectoryName(input) ?? string.Empty;
        prefix = Path.GetFileName(input);

        return !string.IsNullOrWhiteSpace(directory) && Directory.Exists(directory);
    }

    private static bool TryCreatePathSuggestion(string entry, string prefix, out SuggestionCandidate suggestion)
    {
        suggestion = default;

        var name = Path.GetFileName(entry);
        if (string.IsNullOrEmpty(name))
        {
            return false;
        }

        var score = ScoreDisplayName(name, prefix);
        if (score <= 0)
        {
            return false;
        }

        suggestion = new SuggestionCandidate(entry, score + 10, 0);
        return true;
    }

    private static bool LooksLikePath(string input) => input.IndexOfAny(['\\', '/', ':']) >= 0;

    private static string NormalizeResolvedDirectoryPath(string path)
        => PathNormalization.NormalizeDirectoryPath(path);

    private static string GetDirectoryDisplayName(string normalizedDirectoryPath)
    {
        var folderName = Path.GetFileName(normalizedDirectoryPath);
        if (!string.IsNullOrWhiteSpace(folderName))
        {
            return folderName;
        }

        var root = Path.GetPathRoot(normalizedDirectoryPath);
        if (string.IsNullOrWhiteSpace(root))
        {
            return normalizedDirectoryPath;
        }

        var driveName = root.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        if (IsDriveLetterSpecifier(driveName))
        {
            var volumeDisplayName = TryGetDriveVolumeDisplayName(root, driveName);
            if (!string.IsNullOrWhiteSpace(volumeDisplayName))
            {
                return volumeDisplayName;
            }
        }

        return string.IsNullOrWhiteSpace(driveName) ? root : driveName;
    }

    private static bool IsDriveLetterSpecifier(string driveName) =>
        driveName.Length == 2 && char.IsLetter(driveName[0]) && driveName[1] == ':';

    private static string TryGetDriveVolumeDisplayName(string root, string driveName)
    {
        try
        {
            var driveInfo = new DriveInfo(root);
            if (!driveInfo.IsReady)
            {
                return string.Empty;
            }

            var label = driveInfo.VolumeLabel.Trim();
            if (!string.IsNullOrWhiteSpace(label))
            {
                return $"{label} ({driveName})";
            }
        }
        catch (Exception)
        {
            // Fallback to drive letter only when drive metadata isn't accessible.
        }

        return string.Empty;
    }

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

        if (TryGetRegistryStringValue(appKey, "DisplayIcon", out var displayIcon) &&
            TryParseExecutablePath(displayIcon, out path))
        {
            return true;
        }

        if (TryGetRegistryStringValue(appKey, "InstallLocation", out var installLocation) &&
            !string.IsNullOrWhiteSpace(installLocation) &&
            Directory.Exists(installLocation) &&
            TryGetFirstExecutableInDirectory(installLocation, out var candidate))
        {
            path = candidate;
            return true;
        }

        return false;
    }

    private static bool TryGetRegistryStringValue(RegistryKey key, string valueName, [NotNullWhen(true)] out string? value)
    {
        value = null;

        try
        {
            if (key.GetValue(valueName) is not string raw)
            {
                return false;
            }

            value = raw;
            return true;
        }
        catch (ObjectDisposedException)
        {
            return false;
        }
        catch (UnauthorizedAccessException)
        {
            return false;
        }
        catch (SecurityException)
        {
            return false;
        }
        catch (IOException)
        {
            return false;
        }
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

        var commaIndex = cleaned.IndexOf(',');
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

