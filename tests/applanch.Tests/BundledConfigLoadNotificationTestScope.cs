using applanch.Core.Configuration;

namespace applanch.Tests;

internal sealed class BundledConfigLoadNotificationTestScope : IDisposable
{
    private static readonly SemaphoreSlim Gate = new(1, 1);

    private BundledConfigLoadNotificationTestScope()
    {
    }

    internal static BundledConfigLoadNotificationTestScope Enter()
    {
        Gate.Wait();
        BundledConfigLoadNotificationCenter.ResetForTests();
        return new BundledConfigLoadNotificationTestScope();
    }

    public void Dispose()
    {
        BundledConfigLoadNotificationCenter.ResetForTests();
        Gate.Release();
    }
}