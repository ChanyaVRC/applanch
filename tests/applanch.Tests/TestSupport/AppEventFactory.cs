using System.Reflection;
using applanch.Events;

namespace applanch.Tests.TestSupport;

internal static class AppEventFactory
{
    private static readonly ConstructorInfo PrivateConstructor = typeof(AppEvent)
        .GetConstructor(BindingFlags.Instance | BindingFlags.NonPublic, null, Type.EmptyTypes, null)
        ?? throw new InvalidOperationException("AppEvent private constructor not found.");

    internal static AppEvent Create()
        => (AppEvent)PrivateConstructor.Invoke(null);
}
