using Xunit;
using Microsoft.Win32;
using applanch.Infrastructure.Integration;
using applanch.Tests.Infrastructure.Integration.TestDoubles;

namespace applanch.Tests.Infrastructure.Integration;

public class ContextMenuRegistrarTests
{
    private const string ExplorerCommandClassId = "6F1D4B72-8A6C-4B95-98B2-5A7A1A3E52A0";
    private const string ExplorerCommandProgId = "applanch.ExplorerCommand";

    [Fact]
    public void Constructor_Default_CanCreateInstance()
    {
        var registrar = new ContextMenuRegistrar();

        Assert.NotNull(registrar);
    }

    [Fact]
    public void EnsureRegistered_NoExecutablePath_DoesNothing()
    {
        var writer = new RecordingRegistryCommandWriter();
        var explorerRegistrar = new RecordingExplorerCommandRegistrar();
        var registrar = new ContextMenuRegistrar(() => null, static _ => null, writer.WriteCommand, explorerRegistrar.Register, enableLegacyCleanup: false);

        registrar.EnsureRegistered();

        Assert.Empty(writer.Calls);
        Assert.Empty(explorerRegistrar.Calls);
    }

    [Fact]
    public void EnsureRegistered_WritesExpectedTargets_WhenShellExtensionIsUnavailable()
    {
        var writer = new RecordingRegistryCommandWriter();
        var explorerRegistrar = new RecordingExplorerCommandRegistrar();
        var registrar = new ContextMenuRegistrar(() => @"C:\\Apps\\applanch.exe", static _ => null, writer.WriteCommand, explorerRegistrar.Register, enableLegacyCleanup: false);

        registrar.EnsureRegistered();

        Assert.Equal(5, writer.Calls.Count);
        Assert.Contains(writer.Calls, static c => c.KeyPath.Contains("Classes\\AllFileSystemObjects\\shell\\applanch.register", StringComparison.Ordinal));
        Assert.Contains(writer.Calls, static c => c.KeyPath.Contains("Classes\\*\\shell\\applanch.register", StringComparison.Ordinal));
        Assert.Contains(writer.Calls, static c => c.KeyPath.Contains("Classes\\exefile\\shell\\applanch.register", StringComparison.Ordinal));
        Assert.Contains(writer.Calls, static c => c.KeyPath.Contains("Classes\\Directory\\shell\\applanch.register", StringComparison.Ordinal));
        Assert.Contains(writer.Calls, static c => c.KeyPath.Contains("Classes\\Directory\\Background\\shell\\applanch.register", StringComparison.Ordinal));

        Assert.All(writer.Calls, static c => Assert.Contains(App.RegisterArgument, c.Command));
        Assert.All(writer.Calls, static c => Assert.False(c.EnableExplorerCommand));
        Assert.Empty(explorerRegistrar.Calls);
    }

    [Fact]
    public void EnsureRegistered_EnablesExplorerCommand_ForSupportedTargetsWhenShellExtensionIsAvailable()
    {
        var writer = new RecordingRegistryCommandWriter();
        var explorerRegistrar = new RecordingExplorerCommandRegistrar();
        var registrar = new ContextMenuRegistrar(
            () => @"C:\\Apps\\applanch.exe",
            static _ => @"C:\\Apps\\applanch.ShellExtension.comhost.dll",
            writer.WriteCommand,
            explorerRegistrar.Register,
            enableLegacyCleanup: false);

        registrar.EnsureRegistered();

        Assert.Single(explorerRegistrar.Calls);
        Assert.Equal(@"C:\\Apps\\applanch.ShellExtension.comhost.dll", explorerRegistrar.Calls[0]);

        Assert.Equal(4, writer.Calls.Count(static c => c.EnableExplorerCommand));
        Assert.Contains(writer.Calls, static c => c.KeyPath.Contains("Classes\\AllFileSystemObjects\\shell\\applanch.register", StringComparison.Ordinal) && c.EnableExplorerCommand);
        Assert.Contains(writer.Calls, static c => c.KeyPath.Contains("Classes\\*\\shell\\applanch.register", StringComparison.Ordinal) && c.EnableExplorerCommand);
        Assert.Contains(writer.Calls, static c => c.KeyPath.Contains("Classes\\exefile\\shell\\applanch.register", StringComparison.Ordinal) && c.EnableExplorerCommand);
        Assert.Contains(writer.Calls, static c => c.KeyPath.Contains("Classes\\Directory\\shell\\applanch.register", StringComparison.Ordinal) && c.EnableExplorerCommand);
        Assert.Contains(writer.Calls, static c => c.KeyPath.Contains("Classes\\Directory\\Background\\shell\\applanch.register", StringComparison.Ordinal) && !c.EnableExplorerCommand);
    }

    [Fact]
    public void EnsureRegistered_WhenExplorerCommandRegistrationThrowsKnownRegistryError_FallsBackToLegacyRegistration()
    {
        // precondition: shell extension is available
        var writer = new RecordingRegistryCommandWriter();
        var registrar = new ContextMenuRegistrar(
            () => @"C:\\Apps\\applanch.exe",
            static _ => @"C:\\Apps\\applanch.ShellExtension.comhost.dll",
            writer.WriteCommand,
            static _ => throw new UnauthorizedAccessException("Simulated explorer command registration failure"),
            enableLegacyCleanup: false);

        var exception = Record.Exception(registrar.EnsureRegistered);

        Assert.Null(exception);
        Assert.Equal(5, writer.Calls.Count);
        Assert.All(writer.Calls, static c => Assert.False(c.EnableExplorerCommand));
    }

    [Fact]
    public void EnsureRegistered_SkipsExplorerCommand_WhenPackageIsNotRegistered()
    {
        var writer = new RecordingRegistryCommandWriter();
        var explorerRegistrar = new RecordingExplorerCommandRegistrar();
        var registrar = new ContextMenuRegistrar(
            () => @"C:\\Apps\\applanch.exe",
            static _ => @"C:\\Apps\\applanch.ShellExtension.comhost.dll",
            writer.WriteCommand,
            explorerRegistrar.Register,
            enableLegacyCleanup: false,
            deleteRegistrySubKeyTree: null,
            isExplorerCommandAllowed: static () => false);

        registrar.EnsureRegistered();

        Assert.Empty(explorerRegistrar.Calls);
        Assert.Equal(5, writer.Calls.Count);
        Assert.All(writer.Calls, static c => Assert.False(c.EnableExplorerCommand));
    }

    [Fact]
    public void EnsureRegistered_WhenWriterThrows_DoesNotThrow()
    {
        var writer = new ThrowingRegistryCommandWriter(new UnauthorizedAccessException("Simulated registry permission error"));
        var registrar = new ContextMenuRegistrar(() => @"C:\\Apps\\applanch.exe", static _ => null, writer.WriteCommand, static _ => { }, enableLegacyCleanup: false);

        var exception = Record.Exception(registrar.EnsureRegistered);

        Assert.Null(exception);
        Assert.Equal(5, writer.CallCount);
    }

    [Fact]
    public void EnsureRegistered_WhenWriterThrowsSecurityException_DoesNotThrow()
    {
        var writer = new ThrowingRegistryCommandWriter(new System.Security.SecurityException("Simulated security policy error"));
        var registrar = new ContextMenuRegistrar(() => @"C:\\Apps\\applanch.exe", static _ => null, writer.WriteCommand, static _ => { }, enableLegacyCleanup: false);

        var exception = Record.Exception(registrar.EnsureRegistered);

        Assert.Null(exception);
        Assert.Equal(5, writer.CallCount);
    }

    [Fact]
    public void EnsureRegistered_WhenWriterThrowsIOException_DoesNotThrow()
    {
        var writer = new ThrowingRegistryCommandWriter(new IOException("Simulated IO error"));
        var registrar = new ContextMenuRegistrar(() => @"C:\\Apps\\applanch.exe", static _ => null, writer.WriteCommand, static _ => { }, enableLegacyCleanup: false);

        var exception = Record.Exception(registrar.EnsureRegistered);

        Assert.Null(exception);
        Assert.Equal(5, writer.CallCount);
    }

    [Fact]
    public void EnsureRegistered_WhenWriterThrowsUnexpected_Throws()
    {
        var writer = new ThrowingRegistryCommandWriter(new InvalidOperationException("Simulated registry write failure"));
        var registrar = new ContextMenuRegistrar(() => @"C:\\Apps\\applanch.exe", static _ => null, writer.WriteCommand, static _ => { }, enableLegacyCleanup: false);

        Assert.Throws<InvalidOperationException>(registrar.EnsureRegistered);
    }

    [Fact]
    public void WriteRegistryCommand_CreatesExpectedValues_WhenExplorerCommandIsDisabled()
    {
        var testKeyPath = @"Software\\applanch-tests\\registrar-" + Guid.NewGuid().ToString("N");
        var expectedCommand = "\"C:\\Apps\\applanch.exe\" --register \"%1\"";

        try
        {
            var method = typeof(ContextMenuRegistrar).GetMethod("WriteRegistryCommand", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
            Assert.NotNull(method);

            method!.Invoke(null,
            [
                testKeyPath,
                "Menu Text",
                @"C:\Apps\applanch.exe",
                expectedCommand,
                false
            ]);

            using var shellKey = Registry.CurrentUser.OpenSubKey(testKeyPath);
            Assert.NotNull(shellKey);
            Assert.Equal("Menu Text", shellKey!.GetValue(string.Empty) as string);
            Assert.Equal(@"C:\Apps\applanch.exe", shellKey.GetValue("Icon") as string);
            Assert.Null(shellKey.GetValue("ExplorerCommandHandler"));
            Assert.Null(shellKey.GetValue("MultiSelectModel"));

            using var commandKey = shellKey.OpenSubKey("command");
            Assert.NotNull(commandKey);
            Assert.Equal(expectedCommand, commandKey!.GetValue(string.Empty) as string);
        }
        finally
        {
            Registry.CurrentUser.DeleteSubKeyTree(testKeyPath, false);
        }
    }

    [Fact]
    public void WriteRegistryCommand_CreatesExplorerCommandValues_WhenEnabled()
    {
        var testKeyPath = @"Software\\applanch-tests\\registrar-" + Guid.NewGuid().ToString("N");

        try
        {
            var method = typeof(ContextMenuRegistrar).GetMethod("WriteRegistryCommand", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
            Assert.NotNull(method);

            method!.Invoke(null,
            [
                testKeyPath,
                "Menu Text",
                @"C:\Apps\applanch.exe",
                "\"C:\\Apps\\applanch.exe\" --register \"%1\"",
                true
            ]);

            using var shellKey = Registry.CurrentUser.OpenSubKey(testKeyPath);
            Assert.NotNull(shellKey);
            Assert.Equal($"{{{ExplorerCommandClassId}}}", shellKey!.GetValue("ExplorerCommandHandler") as string);
            Assert.Equal("Single", shellKey.GetValue("MultiSelectModel") as string);
        }
        finally
        {
            Registry.CurrentUser.DeleteSubKeyTree(testKeyPath, false);
        }
    }

    [Fact]
    public void RegisterExplorerCommandServer_CreatesExpectedComKeys()
    {
        var classKeyPath = $@"Software\Classes\CLSID\{{{ExplorerCommandClassId}}}";
        var progIdKeyPath = $@"Software\Classes\{ExplorerCommandProgId}";
        var shellExtensionComHostPath = @"C:\Apps\applanch.ShellExtension.comhost.dll";

        try
        {
            var method = typeof(ContextMenuRegistrar).GetMethod("RegisterExplorerCommandServer", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
            Assert.NotNull(method);

            method!.Invoke(null, [shellExtensionComHostPath]);

            using var classKey = Registry.CurrentUser.OpenSubKey(classKeyPath);
            Assert.NotNull(classKey);
            Assert.Equal("Applanch Explorer Command", classKey!.GetValue(string.Empty) as string);
            Assert.Equal(ExplorerCommandProgId, classKey.GetValue("ProgId") as string);

            using var inProcServerKey = Registry.CurrentUser.OpenSubKey(classKeyPath + "\\InprocServer32");
            Assert.NotNull(inProcServerKey);
            Assert.Equal(shellExtensionComHostPath, inProcServerKey!.GetValue(string.Empty) as string);
            Assert.Equal("Both", inProcServerKey.GetValue("ThreadingModel") as string);

            using var progIdKey = Registry.CurrentUser.OpenSubKey(progIdKeyPath);
            Assert.NotNull(progIdKey);
            Assert.Equal($"{{{ExplorerCommandClassId}}}", progIdKey!.GetValue("CLSID") as string);
        }
        finally
        {
            Registry.CurrentUser.DeleteSubKeyTree(classKeyPath, false);
            Registry.CurrentUser.DeleteSubKeyTree(progIdKeyPath, false);
        }
    }

    [Fact]
    public void Unregister_DeletesAllExpectedKeys()
    {
        var deleter = new RecordingRegistryKeyDeleter();
        var registrar = new ContextMenuRegistrar(() => null, static _ => null, static (_, _, _, _, _) => { }, static _ => { }, enableLegacyCleanup: false, deleter.Delete);

        registrar.Unregister();

        Assert.Equal(7, deleter.Calls.Count);
        Assert.Contains(deleter.Calls, static k => k.Contains($@"Classes\CLSID\{{{ExplorerCommandClassId}}}", StringComparison.Ordinal));
        Assert.Contains(deleter.Calls, static k => k.Contains($@"Classes\{ExplorerCommandProgId}", StringComparison.Ordinal));
        Assert.Contains(deleter.Calls, static k => k.Contains(@"Classes\AllFileSystemObjects\shell\applanch.register", StringComparison.Ordinal));
        Assert.Contains(deleter.Calls, static k => k.Contains(@"Classes\*\shell\applanch.register", StringComparison.Ordinal));
        Assert.Contains(deleter.Calls, static k => k.Contains(@"Classes\exefile\shell\applanch.register", StringComparison.Ordinal));
        Assert.Contains(deleter.Calls, static k => k.Contains(@"Classes\Directory\shell\applanch.register", StringComparison.Ordinal));
        Assert.Contains(deleter.Calls, static k => k.Contains(@"Classes\Directory\Background\shell\applanch.register", StringComparison.Ordinal));
    }

    [Fact]
    public void Unregister_WhenDeleterThrowsUnauthorizedAccess_DoesNotThrow()
    {
        var registrar = new ContextMenuRegistrar(
            () => null, static _ => null, static (_, _, _, _, _) => { }, static _ => { }, enableLegacyCleanup: false,
            static _ => throw new UnauthorizedAccessException("Simulated denial"));

        var exception = Record.Exception(registrar.Unregister);

        Assert.Null(exception);
    }

    [Fact]
    public void Unregister_WhenDeleterThrowsSecurityException_DoesNotThrow()
    {
        var registrar = new ContextMenuRegistrar(
            () => null, static _ => null, static (_, _, _, _, _) => { }, static _ => { }, enableLegacyCleanup: false,
            static _ => throw new System.Security.SecurityException("Simulated security error"));

        var exception = Record.Exception(registrar.Unregister);

        Assert.Null(exception);
    }

    [Fact]
    public void Unregister_WhenDeleterThrowsIOException_DoesNotThrow()
    {
        var registrar = new ContextMenuRegistrar(
            () => null, static _ => null, static (_, _, _, _, _) => { }, static _ => { }, enableLegacyCleanup: false,
            static _ => throw new IOException("Simulated IO error"));

        var exception = Record.Exception(registrar.Unregister);

        Assert.Null(exception);
    }

    [Fact]
    public void Unregister_WhenDeleterThrowsUnexpected_Throws()
    {
        var registrar = new ContextMenuRegistrar(
            () => null, static _ => null, static (_, _, _, _, _) => { }, static _ => { }, enableLegacyCleanup: false,
            static _ => throw new InvalidOperationException("Simulated unexpected error"));

        Assert.Throws<InvalidOperationException>(registrar.Unregister);
    }

    [Fact]
    public void ResolveShellExtensionComHostPath_UsesPublishedArtifactsDirectory_WhenArtifactsExistNextToExecutable()
    {
        var rootDirectory = CreateTemporaryDirectory();
        string? resolvedPath = null;
        try
        {
            var executableDirectory = Path.Combine(rootDirectory, "publish");
            Directory.CreateDirectory(executableDirectory);
            File.WriteAllText(Path.Combine(executableDirectory, "applanch.exe"), string.Empty);
            CreateShellExtensionArtifacts(executableDirectory);

            resolvedPath = InvokeResolveShellExtensionComHostPath(Path.Combine(executableDirectory, "applanch.exe"));

            Assert.NotNull(resolvedPath);
            Assert.NotEqual(Path.Combine(executableDirectory, "applanch.ShellExtension.comhost.dll"), resolvedPath);
            Assert.True(File.Exists(resolvedPath));
            Assert.True(File.Exists(Path.Combine(Path.GetDirectoryName(resolvedPath!)!, "applanch.ShellExtension.dll")));
        }
        finally
        {
            DeleteDeploymentDirectory(resolvedPath);
            Directory.Delete(rootDirectory, recursive: true);
        }
    }

    [Fact]
    public void ResolveShellExtensionComHostPath_UsesSiblingProjectOutput_WhenArtifactsAreNotCopiedToAppOutput()
    {
        var rootDirectory = CreateTemporaryDirectory();
        string? resolvedPath = null;
        try
        {
            var executableDirectory = Path.Combine(rootDirectory, "src", "applanch", "bin", "Debug", "net10.0-windows10.0.22000.0");
            Directory.CreateDirectory(executableDirectory);
            File.WriteAllText(Path.Combine(executableDirectory, "applanch.exe"), string.Empty);

            var shellExtensionOutputDirectory = Path.Combine(rootDirectory, "src", "applanch.ShellExtension", "bin", "Debug", "net10.0-windows10.0.22000.0");
            Directory.CreateDirectory(shellExtensionOutputDirectory);
            CreateShellExtensionArtifacts(shellExtensionOutputDirectory);

            resolvedPath = InvokeResolveShellExtensionComHostPath(Path.Combine(executableDirectory, "applanch.exe"));

            Assert.NotNull(resolvedPath);
            Assert.True(File.Exists(resolvedPath));
            Assert.DoesNotContain(Path.Combine("src", "applanch", "bin"), resolvedPath!, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain(Path.Combine("src", "applanch.ShellExtension", "bin"), resolvedPath!, StringComparison.OrdinalIgnoreCase);
        }
        finally
        {
            DeleteDeploymentDirectory(resolvedPath);
            Directory.Delete(rootDirectory, recursive: true);
        }
    }

    private static string InvokeResolveShellExtensionComHostPath(string executablePath)
    {
        var method = typeof(ContextMenuRegistrar).GetMethod("ResolveShellExtensionComHostPath", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
        Assert.NotNull(method);

        return Assert.IsType<string>(method!.Invoke(null, [executablePath]));
    }

    private static void CreateShellExtensionArtifacts(string directoryPath)
    {
        File.WriteAllText(Path.Combine(directoryPath, "applanch.ShellExtension.dll"), "managed");
        File.WriteAllText(Path.Combine(directoryPath, "applanch.ShellExtension.comhost.dll"), "comhost");
        File.WriteAllText(Path.Combine(directoryPath, "applanch.ShellExtension.deps.json"), "{}");
        File.WriteAllText(Path.Combine(directoryPath, "applanch.ShellExtension.runtimeconfig.json"), "{}");
    }

    private static string CreateTemporaryDirectory()
    {
        var path = Path.Combine(Path.GetTempPath(), "applanch-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(path);
        return path;
    }

    private static void DeleteDeploymentDirectory(string? resolvedPath)
    {
        var deploymentDirectory = Path.GetDirectoryName(resolvedPath);
        if (!string.IsNullOrWhiteSpace(deploymentDirectory) && Directory.Exists(deploymentDirectory))
        {
            Directory.Delete(deploymentDirectory, recursive: true);
        }
    }
}


