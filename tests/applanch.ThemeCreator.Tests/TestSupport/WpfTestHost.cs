using System.Runtime.Versioning;
using System.Windows;
using SharedHost = applanch.Tests.Support.WpfTestHost;

namespace applanch.Tests.ThemeCreator.TestSupport;

[SupportedOSPlatform("windows")]
internal static class WpfTestHost
{
    internal static void RunInSta(Action action) => SharedHost.RunInSta(action);

    internal static void EnsureApplication() => SharedHost.EnsureApplication();

    internal static void ShowOffscreen(Window window) => SharedHost.ShowOffscreen(window);

    internal static void DoEvents() => SharedHost.DoEvents();
}
