using applanch.Infrastructure.Integration;
using applanch.Infrastructure.Registry;
using Microsoft.Win32;
using Xunit;

namespace applanch.Tests.Infrastructure.Integration;

public class StartupRegistrationServiceTests
{
    [Fact]
    public void Apply_Enabled_WritesQuotedExecutablePath()
    {
        var runKey = new FakeRunKey();
        var sut = new StartupRegistrationService(new FakeRegistryRuntime(runKey));

        sut.Apply(enabled: true, executablePath: @"C:\Tools\applanch.exe");

        Assert.Equal("\"C:\\Tools\\applanch.exe\"", runKey.StoredValue);
        Assert.False(runKey.DeleteCalled);
        Assert.True(runKey.DisposeCalled);
    }

    [Fact]
    public void Apply_Disabled_WithExistingValue_DeletesEntry()
    {
        var runKey = new FakeRunKey(existingValue: "existing");
        var sut = new StartupRegistrationService(new FakeRegistryRuntime(runKey));

        sut.Apply(enabled: false, executablePath: @"C:\Tools\applanch.exe");

        Assert.True(runKey.DeleteCalled);
        Assert.True(runKey.DisposeCalled);
    }

    [Fact]
    public void Apply_Disabled_WithoutExistingValue_DoesNotDelete()
    {
        var runKey = new FakeRunKey(existingValue: null);
        var sut = new StartupRegistrationService(new FakeRegistryRuntime(runKey));

        sut.Apply(enabled: false, executablePath: @"C:\Tools\applanch.exe");

        Assert.False(runKey.DeleteCalled);
        Assert.True(runKey.DisposeCalled);
    }

    private sealed class FakeRegistryRuntime(IRegistryKey? runKey) : IRegistryRuntime
    {
        public IRegistryKey CurrentUser => runKey ?? new FakeRunKey();

        public IRegistryKey? OpenSubKey(IRegistryKey rootKey, string keyPath, bool writable) => runKey;

        public IRegistryKey? CreateSubKey(IRegistryKey rootKey, string keyPath, bool writable) => runKey;

        public void DeleteSubKeyTree(IRegistryKey rootKey, string keyPath, bool throwOnMissingSubKey)
        {
        }
    }

    private sealed class FakeRunKey(object? existingValue = null) : IRegistryKey
    {
        private object? _value = existingValue;

        public string? StoredValue => _value as string;
        public bool DeleteCalled { get; private set; }
        public bool DisposeCalled { get; private set; }

        public object? GetValue(string name) => _value;

        public void SetValue(string name, object value, RegistryValueKind valueKind)
        {
            _value = value;
        }

        public void DeleteValue(string name, bool throwOnMissingValue)
        {
            DeleteCalled = true;
            _value = null;
        }

        public IRegistryKey? OpenSubKey(string keyPath, bool writable) => null;

        public IRegistryKey? CreateSubKey(string keyPath, bool writable) => null;

        public void DeleteSubKeyTree(string keyPath, bool throwOnMissingSubKey)
        {
        }

        public void Dispose()
        {
            DisposeCalled = true;
        }
    }
}
