using System.IO;
using System.IO.Compression;
using System.Net.Http;
using System.Net.Http.Json;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text.Json;
using applanch.Infrastructure.Storage;
using applanch.Infrastructure.Utilities;

namespace applanch.Infrastructure.Updates;

internal sealed class GitHubAppUpdateService : IAppUpdateService, IDisposable
{
    private const string Owner = "ChanyaVRC";
    private const string Repo = "applanch";
    private const int MaxRetryAttempts = 3;
    private static readonly TimeSpan BaseRetryDelay = TimeSpan.FromMilliseconds(200);

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
    };

    private readonly HttpClient _httpClient;
    private readonly string _currentVersion;
    private readonly bool _debugUpdate;

    public GitHubAppUpdateService()
        : this(CreateDefaultHttpClient(), GetAssemblyVersion(), AppSettings.Load().DebugUpdate)
    {
    }

    internal GitHubAppUpdateService(HttpClient httpClient, string currentVersion, bool debugUpdate = false)
    {
        _httpClient = httpClient;
        _currentVersion = currentVersion;
        _debugUpdate = debugUpdate;
        AppLogger.Instance.Info($"Initialized: currentVersion={currentVersion}, debugUpdate={debugUpdate}");
    }

    public async Task<AppUpdateInfo?> CheckForUpdateAsync(CancellationToken cancellationToken = default)
    {
        var log = AppLogger.Instance;
        var releaseUri = new Uri($"https://api.github.com/repos/{Owner}/{Repo}/releases/latest", UriKind.Absolute);
        log.Info($"Fetching latest release from {releaseUri}");
        using var response = await SendWithRetryAsync(static (client, requestUrl, ct) =>
        {
            var request = new HttpRequestMessage(HttpMethod.Get, requestUrl);
            request.Headers.Accept.ParseAdd("application/vnd.github+json");
            return client.SendAsync(request, ct);
        }, releaseUri, RetryOperation.UpdateMetadata, cancellationToken).ConfigureAwait(false);
        log.Info($"API response: {(int)response.StatusCode} {response.StatusCode}");
        response.EnsureSuccessStatusCode();
        var release = await response.Content.ReadFromJsonAsync<GitHubRelease>(JsonOptions, cancellationToken).ConfigureAwait(false);
        if (release is null)
        {
            log.Info("Release deserialized as null");
            return null;
        }

        var latestVersion = release.TagName.TrimStart('v');
        log.Info($"Latest release: {release.TagName} (parsed: {latestVersion}), assets: {release.Assets.Count}");
        if (!_debugUpdate && !IsNewer(latestVersion, _currentVersion))
        {
            log.Info($"No update needed: {latestVersion} is not newer than {_currentVersion}");
            return null;
        }

        var rid = RuntimeInformation.RuntimeIdentifier;
        var assetName = $"applanch-{latestVersion}-{rid}.zip";
        log.Info($"Looking for asset: {assetName} (RID={rid})");
        var asset = release.Assets.FirstOrDefault(a =>
            string.Equals(a.Name, assetName, StringComparison.OrdinalIgnoreCase));

        if (asset is null)
        {
            var available = string.Join(", ", release.Assets.Select(static a => a.Name));
            log.Info($"Matching asset not found. Available: [{available}]");
            return null;
        }

        log.Info($"Update available: {latestVersion}, download URL: {asset.BrowserDownloadUrl}");
        return new AppUpdateInfo(latestVersion, _currentVersion, asset.BrowserDownloadUrl, release.HtmlUrl);
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
        WriteUpdateScript(scriptPath, currentExePath, extractDir, currentDir);
        log.Info($"Update script written to {scriptPath}");

        System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
        {
            FileName = "cmd.exe",
            Arguments = $"/c \"{scriptPath}\"",
            UseShellExecute = false,
            CreateNoWindow = true,
        });
        log.Info("Update script launched, shutting down for replacement");
    }

    internal async Task<string> DownloadAndExtractAsync(string assetUrl, string tempDir, CancellationToken cancellationToken = default)
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
        var assetUri = new Uri(assetUrl, UriKind.Absolute);
        using (var response = await SendWithRetryAsync(static (client, requestUrl, ct) =>
            client.GetAsync(requestUrl, HttpCompletionOption.ResponseHeadersRead, ct),
            assetUri,
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

    public void Dispose() => _httpClient.Dispose();

    internal static bool IsNewer(string candidate, string current)
    {
        if (!SemanticVersion.TryParse(candidate, out var candidateVersion) ||
            !SemanticVersion.TryParse(current, out var currentVersion))
        {
            return false;
        }

        return candidateVersion.CompareTo(currentVersion) > 0;
    }

    private static void WriteUpdateScript(string scriptPath, string currentExePath, string extractDir, string targetDir)
    {
        var lines = new[]
        {
            "@echo off",
            "timeout /t 2 /nobreak > nul",
            $"xcopy /s /y /q \"{extractDir}\\*\" \"{targetDir}\\\"",
            $"start \"\" \"{currentExePath}\"",
            $"rmdir /s /q \"{Path.GetDirectoryName(scriptPath)}\"",
        };
        File.WriteAllLines(scriptPath, lines);
    }

    private static HttpClient CreateDefaultHttpClient()
    {
        var client = new HttpClient();
        client.DefaultRequestHeaders.UserAgent.ParseAdd($"applanch/{GetAssemblyVersion()}");
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
                AppLogger.Instance.Warn($"Retrying {FormatOperation(operation)} after transient error (attempt {attempt}/{MaxRetryAttempts - 1}): {ex.Message}");
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

    private static string GetAssemblyVersion()
    {
        var version = Assembly.GetExecutingAssembly()
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;

        if (version is not null)
        {
            var plusIndex = version.IndexOf('+');
            if (plusIndex >= 0)
            {
                version = version[..plusIndex];
            }
        }

        return version ?? "0.0.0";
    }
}

