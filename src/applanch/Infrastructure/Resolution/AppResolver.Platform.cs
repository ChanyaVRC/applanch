using System.Diagnostics.CodeAnalysis;
using Microsoft.Win32;
using System.IO;
using System.Runtime.InteropServices;
using System.Security;
using System.Text;

namespace applanch;

internal static partial class AppResolver
{
    private sealed class WindowsAppResolverPlatform
    {
        private static readonly RegistryKey[] SearchHives = [Registry.CurrentUser, Registry.LocalMachine];

        public bool TryResolveFromAppPaths(string candidate, out ResolvedApp resolvedApp)
        {
            foreach (var hive in SearchHives)
            {
                using var key = hive.OpenSubKey($"Software\\Microsoft\\Windows\\CurrentVersion\\App Paths\\{candidate}");
                if (key?.GetValue(string.Empty) is string resolvedPath &&
                    !string.IsNullOrWhiteSpace(resolvedPath) &&
                    File.Exists(resolvedPath))
                {
                    resolvedApp = new ResolvedApp(resolvedPath, Path.GetFileNameWithoutExtension(resolvedPath));
                    return true;
                }
            }

            resolvedApp = default;
            return false;
        }

        public bool TryResolveFromPath(string candidate, out ResolvedApp resolvedApp)
        {
            var buffer = new StringBuilder(4096);
            var result = SearchPath(null, candidate, null, buffer.Length, buffer, out _);
            if (result > 0 && result < buffer.Length)
            {
                var resolvedPath = buffer.ToString();
                if (File.Exists(resolvedPath))
                {
                    resolvedApp = new ResolvedApp(resolvedPath, Path.GetFileNameWithoutExtension(resolvedPath));
                    return true;
                }
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

            return [.. result.DistinctBy(static x => x.Path, StringComparer.OrdinalIgnoreCase)];
        }

        public bool TryEnumerateFileSystemEntries(string directory, [NotNullWhen(true)] out IEnumerable<string>? entries)
        {
            try
            {
                entries = Directory.EnumerateFileSystemEntries(directory);
                return true;
            }
            catch (UnauthorizedAccessException)
            {
                entries = null;
                return false;
            }
            catch (IOException)
            {
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
            catch (UnauthorizedAccessException)
            {
                return;
            }
            catch (SecurityException)
            {
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
            catch (UnauthorizedAccessException)
            {
                return;
            }
            catch (SecurityException)
            {
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

                target.Add(new ResolvedApp(path, displayName));
            }
        }
    }

    [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern int SearchPath(
        string? lpPath,
        string lpFileName,
        string? lpExtension,
        int nBufferLength,
        StringBuilder lpBuffer,
        out IntPtr lpFilePart);
}
