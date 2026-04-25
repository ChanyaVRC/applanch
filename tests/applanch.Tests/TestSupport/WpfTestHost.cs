using System.Runtime.Versioning;
using System.Windows;
using SharedHost = applanch.Tests.Support.WpfTestHost;
using WpfApplication = System.Windows.Application;

namespace applanch.Tests.TestSupport;

[SupportedOSPlatform("windows")]
internal static class WpfTestHost
{
    private static readonly Lock AppInitLock = new();

    internal static void RunInSta(Action action) => SharedHost.RunInSta(action);

    internal static void RunInSta(Action action, TimeSpan timeout) => SharedHost.RunInSta(action, timeout);

    internal static void RunInSta(Func<Task> action) => SharedHost.RunInSta(action);

    internal static void RunInStaAndDrain(Action action) => SharedHost.RunInStaAndDrain(action);

    internal static void ShowOffscreen(Window window) => SharedHost.ShowOffscreen(window);

    internal static void DoEvents() => SharedHost.DoEvents();

    internal static void EnsureAppResources()
    {
        lock (AppInitLock)
        {
            SharedHost.EnsureApplication();

            if (WpfApplication.Current?.Resources["RoundedTextBoxStyle"] is null)
            {
                var dictionary = (ResourceDictionary)WpfApplication.LoadComponent(
                    new Uri("/applanch;component/AppResources.xaml", UriKind.Relative));
                WpfApplication.Current!.Resources.MergedDictionaries.Add(dictionary);
            }
        }
    }
}

