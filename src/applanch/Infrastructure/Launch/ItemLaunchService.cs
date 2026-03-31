using System.Diagnostics;
using System.ComponentModel;
using System.IO;
using System.Windows;
using applanch.Properties;
using applanch.Infrastructure.Utilities;

namespace applanch.Infrastructure.Launch;

internal sealed class ItemLaunchService : IItemLaunchService
{
    private readonly Func<ProcessStartInfo, Process?> _startProcess;

    public ItemLaunchService()
        : this(Process.Start)
    {
    }

    internal ItemLaunchService(Func<ProcessStartInfo, Process?> startProcess)
    {
        _startProcess = startProcess;
    }

    public LaunchExecutionResult TryLaunch(LaunchItemViewModel item, bool runAsAdministrator = false)
    {
        var path = item.FullPath;
        var isDirectory = Directory.Exists(path);
        var isFile = !isDirectory && File.Exists(path);

        if (!isFile && !isDirectory)
        {
            return LaunchExecutionResult.Failed(
                string.Format(Resources.Error_FileNotFound, path),
                MessageBoxImage.Warning);
        }

        var startInfo = new ProcessStartInfo { UseShellExecute = true };
        if (isDirectory)
        {
            startInfo.FileName = "explorer.exe";
            startInfo.Arguments = $"\"{path}\"";
        }
        else
        {
            startInfo.FileName = path;
            startInfo.Arguments = item.Arguments;
        }

        if (runAsAdministrator)
        {
            startInfo.Verb = "runas";
        }

        try
        {
            var process = _startProcess(startInfo);
            if (process is null)
                return LaunchExecutionResult.Failed(Resources.Error_LaunchFailed, MessageBoxImage.Error);

            return LaunchExecutionResult.Success();
        }
        catch (Exception ex)
        {
            if (IsAccessDenied(ex) && TryCreateFallbackStartInfo(path, runAsAdministrator, out var fallback, out var fallbackName))
            {
                try
                {
                    AppLogger.Instance.Warn($"Primary launch denied for '{path}'. Trying fallback: {fallbackName}.");
                    var fallbackProcess = _startProcess(fallback);
                    if (fallbackProcess is not null)
                    {
                        AppLogger.Instance.Info($"Fallback launch succeeded for '{path}' via {fallbackName}.");
                        return LaunchExecutionResult.Success();
                    }

                    AppLogger.Instance.Warn($"Fallback launch returned null process for '{path}' via {fallbackName}.");
                }
                catch (Exception fallbackEx)
                {
                    AppLogger.Instance.Error(fallbackEx, $"Fallback launch failed for '{path}' via {fallbackName}");
                }
            }

            AppLogger.Instance.Error(ex, $"Failed to launch: {path}");
            return LaunchExecutionResult.Failed(string.Format(Resources.Error_LaunchFailedWithMessage, ex.Message), MessageBoxImage.Error);
        }
    }

    private static bool IsAccessDenied(Exception ex)
    {
        if (ex is UnauthorizedAccessException)
        {
            return true;
        }

        if (ex is Win32Exception win32 && win32.NativeErrorCode == 5)
        {
            return true;
        }

        return ex.Message.Contains("access is denied", StringComparison.OrdinalIgnoreCase) ||
               ex.Message.Contains("アクセスが拒否", StringComparison.Ordinal);
    }

    private static bool TryCreateFallbackStartInfo(
        string launchPath,
        bool runAsAdministrator,
        out ProcessStartInfo fallback,
        out string fallbackName)
    {
        if (TryCreateRiotClientFallback(launchPath, runAsAdministrator, out fallback))
        {
            fallbackName = "Riot Client";
            return true;
        }

        if (TryCreateSteamFallback(launchPath, runAsAdministrator, out fallback))
        {
            fallbackName = "Steam";
            return true;
        }

        fallback = default!;
        fallbackName = string.Empty;
        return false;
    }

    private static bool TryCreateRiotClientFallback(string launchPath, bool runAsAdministrator, out ProcessStartInfo fallback)
    {
        fallback = default!;

        if (!string.Equals(Path.GetFileName(launchPath), "VALORANT.exe", StringComparison.OrdinalIgnoreCase) &&
            !string.Equals(Path.GetFileName(launchPath), "LeagueClient.exe", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        var productArg = string.Equals(Path.GetFileName(launchPath), "VALORANT.exe", StringComparison.OrdinalIgnoreCase)
            ? "valorant"
            : "league_of_legends";

        if (!TryFindContainingDirectory(launchPath, "Riot Games", out var riotGamesRoot))
        {
            return false;
        }

        var riotClientPath = Path.Combine(riotGamesRoot, "Riot Client", "RiotClientServices.exe");
        if (!File.Exists(riotClientPath))
        {
            return false;
        }

        fallback = new ProcessStartInfo
        {
            UseShellExecute = true,
            FileName = riotClientPath,
            Arguments = $"--launch-product={productArg} --launch-patchline=live",
        };

        if (runAsAdministrator)
        {
            fallback.Verb = "runas";
        }

        return true;
    }

    private static bool TryCreateSteamFallback(string launchPath, bool runAsAdministrator, out ProcessStartInfo fallback)
    {
        fallback = default!;

        if (!TryFindContainingDirectory(launchPath, "steamapps", out var steamAppsRoot))
        {
            return false;
        }

        var steamRoot = Directory.GetParent(steamAppsRoot)?.FullName;
        if (string.IsNullOrWhiteSpace(steamRoot))
        {
            return false;
        }

        var steamExe = Path.Combine(steamRoot, "steam.exe");
        if (!File.Exists(steamExe))
        {
            return false;
        }

        if (!TryResolveSteamAppId(launchPath, steamAppsRoot, out var appId))
        {
            return false;
        }

        fallback = new ProcessStartInfo
        {
            UseShellExecute = true,
            FileName = steamExe,
            Arguments = $"steam://rungameid/{appId}",
        };

        if (runAsAdministrator)
        {
            fallback.Verb = "runas";
        }

        return true;
    }

    private static bool TryResolveSteamAppId(string launchPath, string steamAppsRoot, out string appId)
    {
        appId = string.Empty;

        var commonRoot = Path.Combine(steamAppsRoot, "common") + Path.DirectorySeparatorChar;
        if (!launchPath.StartsWith(commonRoot, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        var relative = launchPath[commonRoot.Length..];
        var gameDirectory = relative.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)[0];
        if (string.IsNullOrWhiteSpace(gameDirectory))
        {
            return false;
        }

        foreach (var manifestPath in Directory.EnumerateFiles(steamAppsRoot, "appmanifest_*.acf", SearchOption.TopDirectoryOnly))
        {
            if (TryReadSteamManifest(manifestPath, out var manifestAppId, out var installDir) &&
                string.Equals(installDir, gameDirectory, StringComparison.OrdinalIgnoreCase))
            {
                appId = manifestAppId;
                return true;
            }
        }

        return false;
    }

    private static bool TryReadSteamManifest(string manifestPath, out string appId, out string installDir)
    {
        appId = string.Empty;
        installDir = string.Empty;

        foreach (var line in File.ReadLines(manifestPath))
        {
            var trimmed = line.Trim();
            if (trimmed.StartsWith("\"appid\"", StringComparison.OrdinalIgnoreCase))
            {
                appId = ExtractQuotedValue(trimmed);
            }
            else if (trimmed.StartsWith("\"installdir\"", StringComparison.OrdinalIgnoreCase))
            {
                installDir = ExtractQuotedValue(trimmed);
            }
        }

        return !string.IsNullOrWhiteSpace(appId) && !string.IsNullOrWhiteSpace(installDir);
    }

    private static string ExtractQuotedValue(string line)
    {
        var parts = line.Split('"', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        return parts.Length >= 2 ? parts[^1] : string.Empty;
    }

    private static bool TryFindContainingDirectory(string filePath, string targetDirectoryName, out string directoryPath)
    {
        directoryPath = string.Empty;

        var current = Path.GetDirectoryName(filePath);
        while (!string.IsNullOrWhiteSpace(current))
        {
            if (string.Equals(Path.GetFileName(current), targetDirectoryName, StringComparison.OrdinalIgnoreCase))
            {
                directoryPath = current;
                return true;
            }

            current = Directory.GetParent(current)?.FullName;
        }

        return false;
    }
}

