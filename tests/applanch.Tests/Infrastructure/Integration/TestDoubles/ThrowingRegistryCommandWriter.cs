namespace applanch.Tests.Infrastructure.Integration.TestDoubles;

internal sealed class ThrowingRegistryCommandWriter(Exception exceptionToThrow)
{
    public int CallCount { get; private set; }

    public void WriteCommand(string keyPath, string menuText, string iconPath, string command)
    {
        CallCount++;
        throw exceptionToThrow;
    }
}
