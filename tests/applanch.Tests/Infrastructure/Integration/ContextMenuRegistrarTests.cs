using Xunit;
using Microsoft.Win32;
using applanch.Infrastructure.Integration;
using applanch.Tests.Infrastructure.Integration.TestDoubles;

namespace applanch.Tests.Infrastructure.Integration;

public class ContextMenuRegistrarTests
{
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
        var registrar = new ContextMenuRegistrar(() => null, writer.WriteCommand);

        registrar.EnsureRegistered();

        Assert.Empty(writer.Calls);
    }

    [Fact]
    public void EnsureRegistered_WritesExpectedTargets()
    {
        var writer = new RecordingRegistryCommandWriter();
        var registrar = new ContextMenuRegistrar(() => @"C:\\Apps\\applanch.exe", writer.WriteCommand);

        registrar.EnsureRegistered();

        Assert.Equal(4, writer.Calls.Count);
        Assert.Contains(writer.Calls, static c => c.KeyPath.Contains("Classes\\*\\shell\\applanch.register", StringComparison.Ordinal));
        Assert.Contains(writer.Calls, static c => c.KeyPath.Contains("Classes\\exefile\\shell\\applanch.register", StringComparison.Ordinal));
        Assert.Contains(writer.Calls, static c => c.KeyPath.Contains("Classes\\Directory\\shell\\applanch.register", StringComparison.Ordinal));
        Assert.Contains(writer.Calls, static c => c.KeyPath.Contains("Classes\\Directory\\Background\\shell\\applanch.register", StringComparison.Ordinal));

        Assert.All(writer.Calls, static c => Assert.Contains(App.RegisterArgument, c.Command));
    }

    [Fact]
    public void EnsureRegistered_WhenWriterThrows_DoesNotThrow()
    {
        var writer = new ThrowingRegistryCommandWriter(new UnauthorizedAccessException("Simulated registry permission error"));
        var registrar = new ContextMenuRegistrar(() => @"C:\\Apps\\applanch.exe", writer.WriteCommand);

        var exception = Record.Exception(registrar.EnsureRegistered);

        Assert.Null(exception);
        Assert.Equal(4, writer.CallCount);
    }

    [Fact]
    public void EnsureRegistered_WhenWriterThrowsSecurityException_DoesNotThrow()
    {
        var writer = new ThrowingRegistryCommandWriter(new System.Security.SecurityException("Simulated security policy error"));
        var registrar = new ContextMenuRegistrar(() => @"C:\\Apps\\applanch.exe", writer.WriteCommand);

        var exception = Record.Exception(registrar.EnsureRegistered);

        Assert.Null(exception);
        Assert.Equal(4, writer.CallCount);
    }

    [Fact]
    public void EnsureRegistered_WhenWriterThrowsIOException_DoesNotThrow()
    {
        var writer = new ThrowingRegistryCommandWriter(new IOException("Simulated IO error"));
        var registrar = new ContextMenuRegistrar(() => @"C:\\Apps\\applanch.exe", writer.WriteCommand);

        var exception = Record.Exception(registrar.EnsureRegistered);

        Assert.Null(exception);
        Assert.Equal(4, writer.CallCount);
    }

    [Fact]
    public void EnsureRegistered_WhenWriterThrowsUnexpected_Throws()
    {
        var writer = new ThrowingRegistryCommandWriter(new InvalidOperationException("Simulated registry write failure"));
        var registrar = new ContextMenuRegistrar(() => @"C:\\Apps\\applanch.exe", writer.WriteCommand);

        Assert.Throws<InvalidOperationException>(registrar.EnsureRegistered);
    }

    [Fact]
    public void WriteRegistryCommand_CreatesExpectedValues()
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
                expectedCommand
            ]);

            using var shellKey = Registry.CurrentUser.OpenSubKey(testKeyPath);
            Assert.NotNull(shellKey);
            Assert.Equal("Menu Text", shellKey!.GetValue(string.Empty) as string);
            Assert.Equal(@"C:\Apps\applanch.exe", shellKey.GetValue("Icon") as string);

            using var commandKey = shellKey.OpenSubKey("command");
            Assert.NotNull(commandKey);
            Assert.Equal(expectedCommand, commandKey!.GetValue(string.Empty) as string);
        }
        finally
        {
            Registry.CurrentUser.DeleteSubKeyTree(testKeyPath, false);
        }
    }
}


