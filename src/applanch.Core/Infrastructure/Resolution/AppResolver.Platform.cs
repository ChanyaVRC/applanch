using System.Buffers;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Win32;
using System.Runtime.InteropServices;
using System.Security;
using applanch.Core.Infrastructure.Utilities;

namespace applanch.Core.Infrastructure.Resolution;

internal static partial class AppResolver
{
    private sealed class WindowsAppResolverPlatform
    {
        private static readonly RegistryKey[] SearchHives = [Registry.CurrentUser, Registry.LocalMachine];
        private readonly Dictionary<string, ResolvedApp> _appPathsCache = new(StringComparer.OrdinalIgnoreCase);
        private readonly Lock _appPathsCacheLock = new();

        public bool TryResolveFromAppPaths(string candidate, out ResolvedApp resolvedApp)
        {
            lock (_appPathsCacheLock)
            {
                if (_appPathsCache.TryGetValue(candidate, out var cached))
                {
                    if (File.Exists(cached.Path.Value))
                    {
                        resolvedApp = cached;
                        return true;
                    }

                    _appPathsCache.Remove(candidate);
                }
            }

            foreach (var hive in SearchHives)
            {
                using var key = hive.OpenSubKey($"Software\\Microsoft\\Windows\\CurrentVersion\\App Paths\\{candidate}");
                if (key?.GetValue(string.Empty) is string resolvedPath &&
                    !string.IsNullOrWhiteSpace(resolvedPath) &&
                    File.Exists(resolvedPath))
                {
                    resolvedApp = new ResolvedApp(new LaunchPath(resolvedPath), Path.GetFileNameWithoutExtension(resolvedPath));

                    lock (_appPathsCacheLock)
                    {
                        _appPathsCache[candidate] = resolvedApp;
                    }

                    return true;
                }
            }

            resolvedApp = default;
            return false;
        }

        public bool TryResolveFromPath(string candidate, out ResolvedApp resolvedApp)
        {
            const int bufferLength = 4096;
            var buffer = ArrayPool<char>.Shared.Rent(bufferLength);
            try
            {
                var result = SearchPath(null, candidate, null, bufferLength, buffer, out _);
                if (result > 0 && result < bufferLength)
                {
                    var resolvedPath = new string(buffer, 0, result);
                    if (File.Exists(resolvedPath))
                    {
                        resolvedApp = new ResolvedApp(new LaunchPath(resolvedPath), Path.GetFileNameWithoutExtension(resolvedPath));
                        return true;
                    }
                }
            }
            finally
            {
                ArrayPool<char>.Shared.Return(buffer);
            }

            resolvedApp = default;
            return false;
        }

        public IReadOnlyList<ResolvedApp> LoadInstalledApps()
        {
            var result = new List<ResolvedApp>();
            foreach (var hive in SearchHives)
            {
                LoadInstalledAppsFromUninstallRoot(hive, @"Software\Microsoft\Windows\CurrentVersion\Uninstall", result);
                LoadInstalledAppsFromUninstallRoot(hive, @"Software\WOW6432Node\Microsoft\Windows\CurrentVersion\Uninstall", result);
            }

            return [.. result.DistinctBy(static x => x.Path)];
        }

        public bool TryEnumerateFileSystemEntries(string directory, [NotNullWhen(true)] out IEnumerable<string>? entries)
        {
            try
            {
                entries = Directory.EnumerateFileSystemEntries(directory);
                return true;
            }
            catch (UnauthorizedAccessException ex)
            {
                AppLogger.Instance.Warn(ex, $"Access denied while enumerating file system entries in '{directory}'");
                entries = null;
                return false;
            }
            catch (IOException ex)
            {
                AppLogger.Instance.Warn(ex, $"I/O error while enumerating file system entries in '{directory}'");
                entries = null;
                return false;
            }
        }

        private static void LoadInstalledAppsFromUninstallRoot(RegistryKey hive, string subKeyPath, List<ResolvedApp> target)
        {
            RegistryKey? uninstallRoot;
            try
            {
                uninstallRoot = hive.OpenSubKey(subKeyPath);
            }
            catch (UnauthorizedAccessException ex)
            {
                AppLogger.Instance.Warn(ex, $"Access denied while opening uninstall registry root '{hive.Name}\\{subKeyPath}'");
                return;
            }
            catch (SecurityException ex)
            {
                AppLogger.Instance.Warn(ex, $"Security error while opening uninstall registry root '{hive.Name}\\{subKeyPath}'");
                return;
            }

            if (uninstallRoot is null)
                return;

            using var root = uninstallRoot;

            string[] subKeyNames;
            try
            {
                subKeyNames = root.GetSubKeyNames();
            }
            catch (UnauthorizedAccessException ex)
            {
                AppLogger.Instance.Warn(ex, $"Access denied while enumerating uninstall subkeys in '{root.Name}'");
                return;
            }
            catch (SecurityException ex)
            {
                AppLogger.Instance.Warn(ex, $"Security error while enumerating uninstall subkeys in '{root.Name}'");
                return;
            }

            foreach (var appKeyName in subKeyNames)
            {
                RegistryKey? appKey;
                try
                {
                    appKey = root.OpenSubKey(appKeyName);
                }
                catch (UnauthorizedAccessException)
                {
                    continue;
                }
                catch (SecurityException)
                {
                    continue;
                }

                if (appKey is null)
                    continue;

                using var key = appKey;

                if (key.GetValue("DisplayName") is not string rawDisplayName)
                    continue;

                var displayName = rawDisplayName.Trim();
                if (string.IsNullOrWhiteSpace(displayName))
                    continue;

                if (!TryExtractExecutablePath(key, out var path))
                    continue;

                target.Add(new ResolvedApp(new LaunchPath(path), displayName));
            }
        }
    }

    [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern int SearchPath(
        string? lpPath,
        string lpFileName,
        string? lpExtension,
        int nBufferLength,
        char[] lpBuffer,
        out IntPtr lpFilePart);
}

