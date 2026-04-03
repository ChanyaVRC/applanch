using Xunit;
using System.Reflection;
using System.Collections;
using Microsoft.Win32;
using applanch.Infrastructure.Resolution;

namespace applanch.Tests.Infrastructure.Resolution;

public class AppResolverTests
{
    [Fact]
    public void GetSuggestions_MaxResultsZero_ReturnsEmpty()
    {
        var suggestions = AppResolver.GetSuggestions("anything", 0);

        Assert.Empty(suggestions);
    }

    [Fact]
    public void TryResolve_WhitespaceInput_ReturnsFalse()
    {
        var ok = AppResolver.TryResolve("   ", out var resolved);

        Assert.False(ok);
        Assert.Equal(default, resolved);
    }

    [Theory]
    [InlineData("https://example.com", "example.com")]
    [InlineData("http://example.com/path/page", "example.com")]
    public void TryResolve_Url_ResolvesWithExpectedDisplayName(string url, string expectedDisplayName)
    {
        var ok = AppResolver.TryResolve(url, out var resolved);

        Assert.True(ok);
        Assert.Equal(url, resolved.Path.Value);
        Assert.Equal(expectedDisplayName, resolved.DisplayName);
    }

    [Fact]
    public void TryResolve_ExistingFilePath_ResolvesToFile()
    {
        var tempDir = CreateTempDirectory();
        try
        {
            var filePath = Path.Combine(tempDir, "MyTool.exe");
            File.WriteAllText(filePath, "");

            var ok = AppResolver.TryResolve(filePath, out var resolved);

            Assert.True(ok);
            Assert.Equal(filePath, resolved.Path.Value);
            Assert.Equal("MyTool", resolved.DisplayName);
        }
        finally
        {
            Directory.Delete(tempDir, recursive: true);
        }
    }

    [Fact]
    public void TryResolve_ExistingDirectoryPath_ResolvesToDirectory()
    {
        var tempDir = CreateTempDirectory();
        var targetDir = Path.Combine(tempDir, "target-folder");
        Directory.CreateDirectory(targetDir);

        try
        {
            var ok = AppResolver.TryResolve(targetDir, out var resolved);

            Assert.True(ok);
            Assert.Equal(targetDir, resolved.Path.Value);
            Assert.Equal("target-folder", resolved.DisplayName);
        }
        finally
        {
            Directory.Delete(tempDir, recursive: true);
        }
    }

    [Fact]
    public void TryResolve_ExistingDirectoryPathWithTrailingSeparator_ResolvesToDirectory()
    {
        var tempDir = CreateTempDirectory();
        var targetDir = Path.Combine(tempDir, "target-folder");
        Directory.CreateDirectory(targetDir);

        try
        {
            var input = targetDir + Path.DirectorySeparatorChar;
            var ok = AppResolver.TryResolve(input, out var resolved);

            Assert.True(ok);
            Assert.Equal(targetDir, resolved.Path.Value);
            Assert.Equal("target-folder", resolved.DisplayName);
        }
        finally
        {
            Directory.Delete(tempDir, recursive: true);
        }
    }

    [Fact]
    public void TryResolve_DriveRootPath_UsesDriveNameAsDisplayName()
    {
        var tempDir = CreateTempDirectory();
        try
        {
            var driveRoot = Path.GetPathRoot(tempDir);
            Assert.False(string.IsNullOrWhiteSpace(driveRoot));

            var ok = AppResolver.TryResolve(driveRoot!, out var resolved);

            Assert.True(ok);
            Assert.Equal(driveRoot, resolved.Path.Value);
            var driveName = driveRoot!.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            var expectedDisplayName = driveName;

            try
            {
                var label = new DriveInfo(driveRoot).VolumeLabel.Trim();
                if (!string.IsNullOrWhiteSpace(label))
                {
                    expectedDisplayName = $"{label} ({driveName})";
                }
            }
            catch (Exception)
            {
                // If metadata access fails in test environment, fall back to drive letter assertion.
            }

            Assert.Equal(expectedDisplayName, resolved.DisplayName);
        }
        finally
        {
            Directory.Delete(tempDir, recursive: true);
        }
    }

    [Fact]
    public void TryResolve_DriveLetterSpecifier_ResolvesToDriveRootPath()
    {
        var tempDir = CreateTempDirectory();
        try
        {
            var driveRoot = Path.GetPathRoot(tempDir);
            Assert.False(string.IsNullOrWhiteSpace(driveRoot));
            var driveSpecifier = driveRoot!.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);

            var ok = AppResolver.TryResolve(driveSpecifier, out var resolved);

            Assert.True(ok);
            Assert.Equal(driveRoot, resolved.Path.Value);
        }
        finally
        {
            Directory.Delete(tempDir, recursive: true);
        }
    }

    [Fact]
    public void GetSuggestions_PathPrefix_ContainsMatchingEntry()
    {
        var tempDir = CreateTempDirectory();
        try
        {
            var unique = $"zzztst-{Guid.NewGuid():N}";
            var filePath = Path.Combine(tempDir, unique + ".exe");
            File.WriteAllText(filePath, "");

            var input = Path.Combine(tempDir, unique.Substring(0, unique.Length - 6));
            var suggestions = AppResolver.GetSuggestions(input, 10);

            Assert.Contains(filePath, suggestions);
        }
        finally
        {
            Directory.Delete(tempDir, recursive: true);
        }
    }

    [Fact]
    public void GetSuggestions_PathPrefix_RespectsMaxResults()
    {
        var tempDir = CreateTempDirectory();
        try
        {
            for (var i = 0; i < 12; i++)
            {
                var filePath = Path.Combine(tempDir, $"pref-{i:D2}.exe");
                File.WriteAllText(filePath, string.Empty);
            }

            var suggestions = AppResolver.GetSuggestions(Path.Combine(tempDir, "pref-"), 5);

            Assert.True(suggestions.Count <= 5);
        }
        finally
        {
            Directory.Delete(tempDir, recursive: true);
        }
    }

    [Fact]
    public void TryParseExecutablePath_SimpleQuotedPathWithIconIndex_ParsesCorrectly()
    {
        var tempDir = CreateTempDirectory();
        try
        {
            var exePath = Path.Combine(tempDir, "Tool.exe");
            File.WriteAllText(exePath, string.Empty);

            var ok = InvokeTryParseExecutablePath($"\"{exePath}\",0", out var parsedPath);

            Assert.True(ok);
            Assert.Equal(exePath, parsedPath);
        }
        finally
        {
            Directory.Delete(tempDir, recursive: true);
        }
    }

    [Fact]
    public void TryParseExecutablePath_QuotedPathContainingCommaWithIconIndex_ParsesCorrectly()
    {
        var tempDir = CreateTempDirectory();
        try
        {
            var folderWithComma = Path.Combine(tempDir, "Folder,Name");
            Directory.CreateDirectory(folderWithComma);
            var exePath = Path.Combine(folderWithComma, "Tool.exe");
            File.WriteAllText(exePath, string.Empty);

            var ok = InvokeTryParseExecutablePath($"\"{exePath}\",0", out var parsedPath);

            Assert.True(ok);
            Assert.Equal(exePath, parsedPath);
        }
        finally
        {
            Directory.Delete(tempDir, recursive: true);
        }
    }

    [Fact]
    public void TryParseExecutablePath_QuotedPathWithArguments_ParsesCorrectly()
    {
        var tempDir = CreateTempDirectory();
        try
        {
            var folderWithSpaces = Path.Combine(tempDir, "Folder With Spaces");
            Directory.CreateDirectory(folderWithSpaces);
            var exePath = Path.Combine(folderWithSpaces, "Tool.exe");
            File.WriteAllText(exePath, string.Empty);

            var ok = InvokeTryParseExecutablePath($"\"{exePath}\" --with-args", out var parsedPath);

            Assert.True(ok);
            Assert.Equal(exePath, parsedPath);
        }
        finally
        {
            Directory.Delete(tempDir, recursive: true);
        }
    }

    [Fact]
    public void TryParseExecutablePath_NonExePath_ReturnsFalse()
    {
        var ok = InvokeTryParseExecutablePath("C:\\Tools\\tool.txt", out var parsedPath);

        Assert.False(ok);
        Assert.Null(parsedPath);
    }

    [Fact]
    public void TryParseExecutablePath_ExeSubstringInDirectoryName_DoesNotMisparse()
    {
        var tempDir = CreateTempDirectory();
        try
        {
            var folder = Path.Combine(tempDir, "Folder.exe", "Inner");
            Directory.CreateDirectory(folder);
            var exePath = Path.Combine(folder, "Tool.exe");
            File.WriteAllText(exePath, string.Empty);

            var ok = InvokeTryParseExecutablePath(exePath, out var parsedPath);

            Assert.True(ok);
            Assert.Equal(exePath, parsedPath);
        }
        finally
        {
            Directory.Delete(tempDir, recursive: true);
        }
    }

    [Fact]
    public void ExpandCandidates_ForInputWithoutExtension_IncludesInputAndExe()
    {
        var method = typeof(AppResolver).GetMethod("ExpandCandidates", BindingFlags.NonPublic | BindingFlags.Static);
        Assert.NotNull(method);

        var sequence = (IEnumerable)method!.Invoke(null, ["vscode"])!;
        var list = sequence.Cast<string>().ToList();

        Assert.Contains("vscode", list);
        Assert.Contains("vscode.exe", list);
        Assert.Equal(2, list.Count);
    }

    [Fact]
    public void ExpandCandidates_ForInputWithExtension_DoesNotAppendExe()
    {
        var method = typeof(AppResolver).GetMethod("ExpandCandidates", BindingFlags.NonPublic | BindingFlags.Static);
        Assert.NotNull(method);

        var sequence = (IEnumerable)method!.Invoke(null, ["notepad.exe"])!;
        var list = sequence.Cast<string>().ToList();

        Assert.Contains("notepad.exe", list);
        Assert.DoesNotContain("notepad.exe.exe", list);
    }

    [Theory]
    [InlineData("C:\\Tools\\app.exe", true)]
    [InlineData("folder/sub", true)]
    [InlineData("dev:tool", true)]
    [InlineData("firefox", false)]
    public void LooksLikePath_DetectsExpectedValues(string input, bool expected)
    {
        var method = typeof(AppResolver).GetMethod("LooksLikePath", BindingFlags.NonPublic | BindingFlags.Static);
        Assert.NotNull(method);

        var actual = (bool)method!.Invoke(null, [input])!;
        Assert.Equal(expected, actual);
    }

    [Theory]
    [InlineData("notepad", "notepad", 100)]
    [InlineData("notepad++", "note", 80)]
    [InlineData("my-notepad", "note", 50)]
    [InlineData("chrome", "note", 0)]
    [InlineData("chrome", "   ", 0)]
    public void ScoreDisplayName_ReturnsExpectedScore(string displayName, string input, int expected)
    {
        var method = typeof(AppResolver).GetMethod("ScoreDisplayName", BindingFlags.NonPublic | BindingFlags.Static);
        Assert.NotNull(method);

        var actual = (int)method!.Invoke(null, [displayName, input])!;
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void ExtractExecutablePathCandidate_QuotedWithArguments_ReturnsExePath()
    {
        var method = typeof(AppResolver).GetMethod("ExtractExecutablePathCandidate", BindingFlags.NonPublic | BindingFlags.Static);
        Assert.NotNull(method);

        var raw = "\"C:\\Program Files\\App\\tool.exe\" --arg";
        var actual = (string)method!.Invoke(null, [raw])!;

        Assert.Equal(@"C:\Program Files\App\tool.exe", actual);
    }

    [Fact]
    public void Platform_TryEnumerateFileSystemEntries_MissingDirectory_ReturnsFalse()
    {
        var platformType = typeof(AppResolver).GetNestedType("WindowsAppResolverPlatform", BindingFlags.NonPublic);
        Assert.NotNull(platformType);

        var platform = Activator.CreateInstance(platformType!)!;
        var method = platformType!.GetMethod("TryEnumerateFileSystemEntries", BindingFlags.Public | BindingFlags.Instance);
        Assert.NotNull(method);

        var missingDirectory = Path.Combine(Path.GetTempPath(), "applanch-missing-" + Guid.NewGuid().ToString("N"));
        object?[] args = [missingDirectory, null];
        var ok = (bool)method!.Invoke(platform, args)!;

        Assert.False(ok);
        Assert.Null(args[1]);
    }

    [Fact]
    public void Platform_TryEnumerateFileSystemEntries_ExistingDirectory_ReturnsTrueWithEntries()
    {
        var tempDir = CreateTempDirectory();
        try
        {
            var filePath = Path.Combine(tempDir, "tool.exe");
            File.WriteAllText(filePath, string.Empty);

            var platformType = typeof(AppResolver).GetNestedType("WindowsAppResolverPlatform", BindingFlags.NonPublic);
            Assert.NotNull(platformType);

            var platform = Activator.CreateInstance(platformType!)!;
            var method = platformType!.GetMethod("TryEnumerateFileSystemEntries", BindingFlags.Public | BindingFlags.Instance);
            Assert.NotNull(method);

            object?[] args = [tempDir, null];
            var ok = (bool)method!.Invoke(platform, args)!;

            Assert.True(ok);
            var entries = ((IEnumerable<string>)args[1]!).ToList();
            Assert.Contains(filePath, entries);
        }
        finally
        {
            Directory.Delete(tempDir, recursive: true);
        }
    }

    [Fact]
    public void Platform_TryResolveFromPath_UnknownName_ReturnsFalse()
    {
        var platformType = typeof(AppResolver).GetNestedType("WindowsAppResolverPlatform", BindingFlags.NonPublic);
        Assert.NotNull(platformType);

        var platform = Activator.CreateInstance(platformType!)!;
        var method = platformType!.GetMethod("TryResolveFromPath", BindingFlags.Public | BindingFlags.Instance);
        Assert.NotNull(method);

        object?[] args = ["this-should-not-exist-" + Guid.NewGuid().ToString("N") + ".exe", null];
        var ok = (bool)method!.Invoke(platform, args)!;

        Assert.False(ok);
    }

    [Fact]
    public void Platform_TryResolveFromAppPaths_WhenRegisteredInCurrentUser_ReturnsTrue()
    {
        var tempDir = CreateTempDirectory();
        var candidate = "applanch-test-resolver.exe";
        var appPathsKey = $"Software\\Microsoft\\Windows\\CurrentVersion\\App Paths\\{candidate}";

        try
        {
            var exePath = Path.Combine(tempDir, candidate);
            File.WriteAllText(exePath, string.Empty);

            using var key = Registry.CurrentUser.CreateSubKey(appPathsKey);
            Assert.NotNull(key);
            key!.SetValue(string.Empty, exePath);

            var platformType = typeof(AppResolver).GetNestedType("WindowsAppResolverPlatform", BindingFlags.NonPublic);
            Assert.NotNull(platformType);

            var platform = Activator.CreateInstance(platformType!)!;
            var method = platformType!.GetMethod("TryResolveFromAppPaths", BindingFlags.Public | BindingFlags.Instance);
            Assert.NotNull(method);

            object?[] args = [candidate, null];
            var ok = (bool)method!.Invoke(platform, args)!;

            Assert.True(ok);
            var resolved = (ResolvedApp)args[1]!;
            Assert.Equal(exePath, resolved.Path.Value);
            Assert.Equal(Path.GetFileNameWithoutExtension(exePath), resolved.DisplayName);
        }
        finally
        {
            Registry.CurrentUser.DeleteSubKeyTree(appPathsKey, false);
            Directory.Delete(tempDir, recursive: true);
        }
    }

    [Fact]
    public void Platform_TryResolveFromAppPaths_MissingEntry_ReturnsFalse()
    {
        var platformType = typeof(AppResolver).GetNestedType("WindowsAppResolverPlatform", BindingFlags.NonPublic);
        Assert.NotNull(platformType);

        var platform = Activator.CreateInstance(platformType!)!;
        var method = platformType!.GetMethod("TryResolveFromAppPaths", BindingFlags.Public | BindingFlags.Instance);
        Assert.NotNull(method);

        object?[] args = ["applanch-missing-" + Guid.NewGuid().ToString("N") + ".exe", null];
        var ok = (bool)method!.Invoke(platform, args)!;

        Assert.False(ok);
    }

    [Fact]
    public void TryGetFirstExecutableInDirectory_WhenExeExists_ReturnsTrue()
    {
        var tempDir = CreateTempDirectory();
        try
        {
            var exePath = Path.Combine(tempDir, "tool.exe");
            File.WriteAllText(exePath, string.Empty);

            var method = typeof(AppResolver).GetMethod("TryGetFirstExecutableInDirectory", BindingFlags.NonPublic | BindingFlags.Static);
            Assert.NotNull(method);

            object?[] args = [tempDir, null];
            var ok = (bool)method!.Invoke(null, args)!;

            Assert.True(ok);
            Assert.Equal(exePath, (string)args[1]!);
        }
        finally
        {
            Directory.Delete(tempDir, recursive: true);
        }
    }

    [Fact]
    public void TryGetFirstExecutableInDirectory_MissingDirectory_ReturnsFalse()
    {
        var method = typeof(AppResolver).GetMethod("TryGetFirstExecutableInDirectory", BindingFlags.NonPublic | BindingFlags.Static);
        Assert.NotNull(method);

        object?[] args = [Path.Combine(Path.GetTempPath(), "missing-" + Guid.NewGuid().ToString("N")), null];
        var ok = (bool)method!.Invoke(null, args)!;

        Assert.False(ok);
    }

    [Fact]
    public void TryExtractExecutablePath_UsesDisplayIcon_WhenValid()
    {
        var tempDir = CreateTempDirectory();
        var keyName = "Software\\applanch-tests\\" + Guid.NewGuid().ToString("N");
        try
        {
            var exePath = Path.Combine(tempDir, "App.exe");
            File.WriteAllText(exePath, string.Empty);

            using var key = Registry.CurrentUser.CreateSubKey(keyName);
            Assert.NotNull(key);
            key!.SetValue("DisplayIcon", "\"" + exePath + "\",0");

            var method = typeof(AppResolver).GetMethod("TryExtractExecutablePath", BindingFlags.NonPublic | BindingFlags.Static);
            Assert.NotNull(method);

            object?[] args = [key, null];
            var ok = (bool)method!.Invoke(null, args)!;

            Assert.True(ok);
            Assert.Equal(exePath, (string)args[1]!);
        }
        finally
        {
            Registry.CurrentUser.DeleteSubKeyTree(keyName, false);
            Directory.Delete(tempDir, recursive: true);
        }
    }

    [Fact]
    public void TryExtractExecutablePath_UsesInstallLocationFallback_WhenDisplayIconMissing()
    {
        var tempDir = CreateTempDirectory();
        var keyName = "Software\\applanch-tests\\" + Guid.NewGuid().ToString("N");
        try
        {
            var exePath = Path.Combine(tempDir, "App.exe");
            File.WriteAllText(exePath, string.Empty);

            using var key = Registry.CurrentUser.CreateSubKey(keyName);
            Assert.NotNull(key);
            key!.SetValue("InstallLocation", tempDir);

            var method = typeof(AppResolver).GetMethod("TryExtractExecutablePath", BindingFlags.NonPublic | BindingFlags.Static);
            Assert.NotNull(method);

            object?[] args = [key, null];
            var ok = (bool)method!.Invoke(null, args)!;

            Assert.True(ok);
            Assert.Equal(exePath, (string)args[1]!);
        }
        finally
        {
            Registry.CurrentUser.DeleteSubKeyTree(keyName, false);
            Directory.Delete(tempDir, recursive: true);
        }
    }

    [Fact]
    public void TryExtractExecutablePath_NoValidSource_ReturnsFalse()
    {
        var keyName = "Software\\applanch-tests\\" + Guid.NewGuid().ToString("N");
        try
        {
            using var key = Registry.CurrentUser.CreateSubKey(keyName);
            Assert.NotNull(key);
            key!.SetValue("DisplayName", "OnlyName");

            var method = typeof(AppResolver).GetMethod("TryExtractExecutablePath", BindingFlags.NonPublic | BindingFlags.Static);
            Assert.NotNull(method);

            object?[] args = [key, null];
            var ok = (bool)method!.Invoke(null, args)!;

            Assert.False(ok);
            Assert.Null(args[1]);
        }
        finally
        {
            Registry.CurrentUser.DeleteSubKeyTree(keyName, false);
        }
    }

    [Fact]
    public void TryExtractExecutablePath_DisposedKey_ReturnsFalse()
    {
        var keyName = "Software\\applanch-tests\\" + Guid.NewGuid().ToString("N");
        try
        {
            var key = Registry.CurrentUser.CreateSubKey(keyName);
            Assert.NotNull(key);
            key!.Dispose();

            var method = typeof(AppResolver).GetMethod("TryExtractExecutablePath", BindingFlags.NonPublic | BindingFlags.Static);
            Assert.NotNull(method);

            object?[] args = [key, null];
            var ok = (bool)method!.Invoke(null, args)!;

            Assert.False(ok);
            Assert.Null(args[1]);
        }
        finally
        {
            Registry.CurrentUser.DeleteSubKeyTree(keyName, false);
        }
    }

    [Fact]
    public void LoadInstalledAppsFromUninstallRoot_WhenEntryIsValid_AddsApp()
    {
        var tempDir = CreateTempDirectory();
        var rootPath = @"Software\\applanch-tests\\uninstall-" + Guid.NewGuid().ToString("N");
        try
        {
            var exePath = Path.Combine(tempDir, "InstalledTool.exe");
            File.WriteAllText(exePath, string.Empty);

            using (var root = Registry.CurrentUser.CreateSubKey(rootPath))
            {
                Assert.NotNull(root);
                using var app = root!.CreateSubKey("App1");
                Assert.NotNull(app);
                app!.SetValue("DisplayName", "Installed Tool");
                app.SetValue("DisplayIcon", "\"" + exePath + "\",0");
            }

            var platformType = typeof(AppResolver).GetNestedType("WindowsAppResolverPlatform", BindingFlags.NonPublic);
            Assert.NotNull(platformType);

            var method = platformType!.GetMethod("LoadInstalledAppsFromUninstallRoot", BindingFlags.NonPublic | BindingFlags.Static);
            Assert.NotNull(method);

            var target = new List<ResolvedApp>();
            method!.Invoke(null, [Registry.CurrentUser, rootPath, target]);

            var appResult = Assert.Single(target);
            Assert.Equal(exePath, appResult.Path.Value);
            Assert.Equal("Installed Tool", appResult.DisplayName);
        }
        finally
        {
            Registry.CurrentUser.DeleteSubKeyTree(rootPath, false);
            Directory.Delete(tempDir, recursive: true);
        }
    }

    [Fact]
    public void LoadInstalledAppsFromUninstallRoot_BlankDisplayName_IsIgnored()
    {
        var rootPath = @"Software\\applanch-tests\\uninstall-" + Guid.NewGuid().ToString("N");
        try
        {
            using (var root = Registry.CurrentUser.CreateSubKey(rootPath))
            {
                Assert.NotNull(root);
                using var app = root!.CreateSubKey("App1");
                Assert.NotNull(app);
                app!.SetValue("DisplayName", "   ");
            }

            var platformType = typeof(AppResolver).GetNestedType("WindowsAppResolverPlatform", BindingFlags.NonPublic);
            Assert.NotNull(platformType);

            var method = platformType!.GetMethod("LoadInstalledAppsFromUninstallRoot", BindingFlags.NonPublic | BindingFlags.Static);
            Assert.NotNull(method);

            var target = new List<ResolvedApp>();
            method!.Invoke(null, [Registry.CurrentUser, rootPath, target]);

            Assert.Empty(target);
        }
        finally
        {
            Registry.CurrentUser.DeleteSubKeyTree(rootPath, false);
        }
    }

    private static bool InvokeTryParseExecutablePath(string raw, out string? path)
    {
        var method = typeof(AppResolver).GetMethod("TryParseExecutablePath", BindingFlags.NonPublic | BindingFlags.Static);
        Assert.NotNull(method);

        object?[] args = [raw, null];
        var ok = (bool)method!.Invoke(null, args)!;
        path = (string?)args[1];
        return ok;
    }

    private static string CreateTempDirectory()
    {
        var path = Path.Combine(Path.GetTempPath(), "applanch-tests-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(path);
        return path;
    }
}


