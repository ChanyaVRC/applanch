using System.IO;
using System.IO.Compression;
using System.Net.Http;
using System.Net.Http.Json;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using System.Text.Json;
using applanch.Infrastructure.Storage;
using applanch.Settings;
using applanch.Core.Utilities;
using applanch.Infrastructure.Utilities;

using applanch.Updates;

namespace applanch.Infrastructure.Updates;

internal sealed class GitHubAppUpdateService : IAppUpdateService
{
    private const string Owner = "ChanyaVRC";
    private const string Repo = "applanch";
    private const int MaxRetryAttempts = 3;
    private static readonly TimeSpan BaseRetryDelay = TimeSpan.FromMilliseconds(200);

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
    };
    private static readonly Lock ReleasesCacheLock = new();
    private static Task<IReadOnlyList<GitHubRelease>>? CachedReleasesTask;
    private static readonly Lazy<HttpClient> DefaultHttpClient = new(CreateDefaultHttpClient);

    private readonly HttpClient _httpClient;
    private readonly SemanticVersion _currentVersion;
    private readonly bool _debugUpdate;
    private readonly bool _allowPrereleaseUpdates;

    public GitHubAppUpdateService()
        : this(AppSettingsProvider.Current)
    {
    }

    private GitHubAppUpdateService(AppSettings settings)
        : this(settings.DebugUpdate, settings.AllowPrereleaseUpdates)
    {
    }

    public GitHubAppUpdateService(bool debugUpdate, bool allowPrereleaseUpdates)
        : this(DefaultHttpClient.Value, AppVersionProvider.CurrentVersion, debugUpdate, allowPrereleaseUpdates)
    {
    }

    internal GitHubAppUpdateService(HttpClient httpClient, SemanticVersion currentVersion, bool debugUpdate, bool allowPrereleaseUpdates)
    {
        _httpClient = httpClient;
        _currentVersion = currentVersion;
        _debugUpdate = debugUpdate;
        _allowPrereleaseUpdates = allowPrereleaseUpdates;
        AppLogger.Instance.Info($"Initialized: currentVersion={currentVersion}, debugUpdate={debugUpdate}, allowPrereleaseUpdates={allowPrereleaseUpdates}");
    }

    public async Task<IReadOnlyList<AppUpdateInfo>> GetAvailableUpdatesAsync(CancellationToken cancellationToken = default)
    {
        var releases = await FetchReleasesAsync(cancellationToken).ConfigureAwait(false);
        var availableUpdates = new List<AppUpdateInfo>();

        foreach (var release in releases)
        {
            if (!ShouldIncludeRelease(release) ||
                !TryCreateUpdateInfo(release, out var update) ||
                update.NewVersion == _currentVersion)
            {
                continue;
            }

            availableUpdates.Add(update);
        }

        return availableUpdates;
    }

    public async Task<AppUpdateInfo?> CheckForUpdateAsync(CancellationToken cancellationToken = default)
    {
        var releases = await FetchReleasesAsync(cancellationToken).ConfigureAwait(false);
        foreach (var listedRelease in releases)
        {
            if (!ShouldIncludeRelease(listedRelease) || !TryCreateUpdateInfo(listedRelease, out var listedUpdate))
            {
                continue;
            }

            if (!_debugUpdate && listedUpdate.NewVersion.CompareTo(_currentVersion) <= 0)
            {
                continue;
            }

            AppLogger.Instance.Info($"Update available: {listedUpdate.NewVersion}, download URL: {listedUpdate.AssetDownloadUrl}");
            return listedUpdate;
        }

        AppLogger.Instance.Info("No eligible update found in releases list");
        return null;
    }

    public async Task ApplyUpdateAsync(AppUpdateInfo update, CancellationToken cancellationToken = default)
    {
        var log = AppLogger.Instance;
        log.Info($"Applying update: {update.CurrentVersion} -> {update.NewVersion}");
        var tempDir = Path.Combine(Path.GetTempPath(), $"applanch-update-{update.NewVersion}");
        var extractDir = await DownloadAndExtractAsync(update.AssetDownloadUrl, tempDir, cancellationToken).ConfigureAwait(false);

        var currentExePath = Environment.ProcessPath
            ?? throw new InvalidOperationException("Cannot determine current executable path.");
        var currentDir = Path.GetDirectoryName(currentExePath)!;
        log.Info($"Current exe: {currentExePath}, target dir: {currentDir}");

        var scriptPath = Path.Combine(tempDir, "apply-update.cmd");
        WriteUpdateScript(scriptPath, Process.GetCurrentProcess().Id, currentExePath, extractDir, currentDir);
        log.Info($"Update script written to {scriptPath}");

        Process.Start(new ProcessStartInfo
        {
            FileName = "cmd.exe",
            Arguments = $"/c \"{scriptPath}\"",
            UseShellExecute = false,
            CreateNoWindow = true,
        });
        log.Info("Update script launched, shutting down for replacement");
    }

    internal async Task<string> DownloadAndExtractAsync(Uri assetUrl, string tempDir, CancellationToken cancellationToken = default)
    {
        var log = AppLogger.Instance;
        log.Info($"Downloading from {assetUrl}");
        log.Info($"Temp dir: {tempDir}");

        if (Directory.Exists(tempDir))
        {
            Directory.Delete(tempDir, true);
        }

        Directory.CreateDirectory(tempDir);

        var zipPath = Path.Combine(tempDir, "update.zip");
        using (var response = await SendWithRetryAsync(static (client, requestUrl, ct) =>
            client.GetAsync(requestUrl, HttpCompletionOption.ResponseHeadersRead, ct),
            assetUrl,
            RetryOperation.UpdatePackage,
            cancellationToken).ConfigureAwait(false))
        {
            log.Info($"Download response: {(int)response.StatusCode} {response.StatusCode}, Content-Type: {response.Content.Headers.ContentType}");
            response.EnsureSuccessStatusCode();
            using var fs = new FileStream(zipPath, FileMode.Create, FileAccess.Write, FileShare.None);
            await response.Content.CopyToAsync(fs, cancellationToken).ConfigureAwait(false);
        }

        var zipSize = new FileInfo(zipPath).Length;
        log.Info($"Downloaded ZIP: {zipPath} ({zipSize} bytes)");

        var extractDir = Path.Combine(tempDir, "extracted");
        log.Info($"Extracting to {extractDir}");
        ZipFile.ExtractToDirectory(zipPath, extractDir);

        var extractedFiles = Directory.GetFiles(extractDir, "*", SearchOption.AllDirectories);
        log.Info($"Extracted {extractedFiles.Length} files");
        return extractDir;
    }

    internal static bool IsNewer(SemanticVersion candidate, SemanticVersion current) =>
        candidate.CompareTo(current) > 0;

    private bool ShouldIncludeRelease(GitHubRelease release) =>
        _allowPrereleaseUpdates || !release.Prerelease;

    private async Task<IReadOnlyList<GitHubRelease>> FetchReleasesAsync(CancellationToken cancellationToken)
    {
        var fetchTask = GetOrCreateCachedReleasesTask();
        try
        {
            return await fetchTask.WaitAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch
        {
            InvalidateReleasesCache(fetchTask);
            throw;
        }
    }

    private Task<IReadOnlyList<GitHubRelease>> GetOrCreateCachedReleasesTask()
    {
        if (CachedReleasesTask is not null)
        {
            return CachedReleasesTask;
        }

        lock (ReleasesCacheLock)
        {
            CachedReleasesTask ??= FetchReleasesFromGitHubAsync(CancellationToken.None);
            return CachedReleasesTask;
        }
    }

    private void InvalidateReleasesCache(Task<IReadOnlyList<GitHubRelease>> failedTask)
    {
        lock (ReleasesCacheLock)
        {
            if (ReferenceEquals(CachedReleasesTask, failedTask))
            {
                CachedReleasesTask = null;
            }
        }
    }

    internal static void ClearReleasesCache()
    {
        lock (ReleasesCacheLock)
        {
            CachedReleasesTask = null;
        }
    }

    private async Task<IReadOnlyList<GitHubRelease>> FetchReleasesFromGitHubAsync(CancellationToken cancellationToken)
    {
        var releasesUri = new Uri($"https://api.github.com/repos/{Owner}/{Repo}/releases", UriKind.Absolute);
        AppLogger.Instance.Info($"Fetching releases from {releasesUri}");
        using var response = await SendWithRetryAsync(static (client, requestUrl, ct) =>
        {
            var request = new HttpRequestMessage(HttpMethod.Get, requestUrl);
            request.Headers.Accept.ParseAdd("application/vnd.github+json");
            return client.SendAsync(request, ct);
        }, releasesUri, RetryOperation.UpdateMetadata, cancellationToken).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<List<GitHubRelease>>(JsonOptions, cancellationToken).ConfigureAwait(false) ?? [];
    }

    private bool TryCreateUpdateInfo(
        GitHubRelease release,
        [NotNullWhen(true)] out AppUpdateInfo? update)
    {
        update = null;
        var releaseVersionText = release.TagName.TrimStart('v');
        if (!SemanticVersion.TryParse(releaseVersionText, out var releaseVersion))
        {
            AppLogger.Instance.Warn($"Release tag '{release.TagName}' is not a valid semantic version.");
            return false;
        }

        AppLogger.Instance.Info($"Release candidate: {release.TagName} (parsed: {releaseVersion}), assets: {release.Assets.Count}");

        var rid = RuntimeInformation.RuntimeIdentifier;
        var assetName = $"applanch-{releaseVersion}-{rid}.zip";
        var asset = release.Assets.FirstOrDefault(a =>
            string.Equals(a.Name, assetName, StringComparison.OrdinalIgnoreCase));

        if (asset is null)
        {
            var available = string.Join(", ", release.Assets.Select(static a => a.Name));
            AppLogger.Instance.Info($"Matching asset not found for {release.TagName}. Available: [{available}]");
            return false;
        }

        if (!Uri.TryCreate(asset.BrowserDownloadUrl, UriKind.Absolute, out var assetDownloadUrl) ||
            !Uri.TryCreate(release.HtmlUrl, UriKind.Absolute, out var releaseUrl))
        {
            AppLogger.Instance.Warn($"Release metadata contains invalid URL values for {release.TagName}.");
            return false;
        }

        update = new AppUpdateInfo(releaseVersion, _currentVersion, assetDownloadUrl, releaseUrl);
        return true;
    }

    internal static string[] BuildUpdateScriptLines(int processId, string currentExePath, string extractDir, string targetDir, string cleanupDir)
    {
        return
        [
            "@echo off",
            "setlocal",
            ":wait_for_exit",
            $"tasklist /FI \"PID eq {processId}\" 2>NUL | find \"{processId}\" >NUL",
            "if not errorlevel 1 (",
            "  timeout /t 1 /nobreak > nul",
            "  goto wait_for_exit",
            ")",
            $"robocopy \"{extractDir}\" \"{targetDir}\" /e /r:5 /w:1 /nfl /ndl /njh /njs /nc /ns /np > nul",
            "if errorlevel 8 exit /b %errorlevel%",
            $"start \"\" \"{currentExePath}\"",
            $"rmdir /s /q \"{cleanupDir}\"",
            "endlocal",
        ];
    }

    private static void WriteUpdateScript(string scriptPath, int processId, string currentExePath, string extractDir, string targetDir)
    {
        var lines = BuildUpdateScriptLines(
            processId,
            currentExePath,
            extractDir,
            targetDir,
            Path.GetDirectoryName(scriptPath) ?? Path.GetTempPath());

        File.WriteAllLines(scriptPath, lines);
    }

    private static HttpClient CreateDefaultHttpClient()
    {
        var client = new HttpClient();
        client.DefaultRequestHeaders.UserAgent.ParseAdd($"applanch/{AppVersionProvider.CurrentVersion}");
        return client;
    }

    private async Task<HttpResponseMessage> SendWithRetryAsync(
        Func<HttpClient, Uri, CancellationToken, Task<HttpResponseMessage>> send,
        Uri url,
        RetryOperation operation,
        CancellationToken cancellationToken)
    {
        for (var attempt = 1; ; attempt++)
        {
            try
            {
                return await send(_httpClient, url, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex) when (ShouldRetry(ex, cancellationToken) && attempt < MaxRetryAttempts)
            {
                var delay = TimeSpan.FromMilliseconds(BaseRetryDelay.TotalMilliseconds * Math.Pow(2, attempt - 1));
                AppLogger.Instance.Warn(ex, $"Retrying {FormatOperation(operation)} after transient error (attempt {attempt}/{MaxRetryAttempts - 1})");
                await Task.Delay(delay, cancellationToken).ConfigureAwait(false);
            }
        }
    }

    private static bool ShouldRetry(Exception ex, CancellationToken cancellationToken)
    {
        if (ex is OperationCanceledException)
        {
            return !cancellationToken.IsCancellationRequested;
        }

        return ex is HttpRequestException or IOException;
    }

    private static string FormatOperation(RetryOperation operation)
    {
        return operation switch
        {
            RetryOperation.UpdateMetadata => "update metadata",
            RetryOperation.UpdatePackage => "update package",
            _ => "update operation",
        };
    }

    private enum RetryOperation
    {
        UpdateMetadata,
        UpdatePackage,
    }

}