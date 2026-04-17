using Microsoft.Win32;
namespace applanch.Infrastructure.Integration;

internal sealed class StartupRegistrationService
{
    private const string EntryName = "applanch";
    private readonly IStartupRegistrationRuntime _runtime;

    public StartupRegistrationService()
        : this(new StartupRegistrationRuntime())
    {
    }

    internal StartupRegistrationService(IStartupRegistrationRuntime runtime)
    {
        _runtime = runtime;
    }

    public void Apply(bool enabled, string executablePath)
    {
        using var runKey = _runtime.OpenRunKey();

        if (runKey is null)
        {
            return;
        }

        if (enabled)
        {
            SetStartupValue(runKey, executablePath);
            return;
        }

        RemoveStartupValue(runKey);
    }

    private static void SetStartupValue(IStartupRunKey runKey, string executablePath)
        => runKey.SetValue(EntryName, Quote(executablePath), RegistryValueKind.String);

    private static void RemoveStartupValue(IStartupRunKey runKey)
    {
        if (runKey.GetValue(EntryName) is null)
        {
            return;
        }

        runKey.DeleteValue(EntryName, throwOnMissingValue: false);
    }

    private static string Quote(string value) => $"\"{value}\"";
}
