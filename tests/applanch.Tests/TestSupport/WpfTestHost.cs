using System.Runtime.ExceptionServices;
using System.Runtime.Versioning;
using System.Windows;
using System.Windows.Threading;
using WpfApplication = System.Windows.Application;

namespace applanch.Tests.TestSupport;

[SupportedOSPlatform("windows")]
internal static class WpfTestHost
{
    private static readonly Lock AppInitLock = new();

    internal static void RunInSta(Action action)
    {
        RunInSta(action, Timeout.InfiniteTimeSpan);
    }

    internal static void RunInSta(Action action, TimeSpan timeout)
    {
        Exception? captured = null;

        var thread = new Thread(() =>
        {
            try
            {
                action();
            }
            catch (Exception ex)
            {
                captured = ex;
            }
        });

        thread.SetApartmentState(ApartmentState.STA);
        thread.IsBackground = true;
        thread.Start();
        bool completed;
        if (timeout == Timeout.InfiniteTimeSpan)
        {
            thread.Join();
            completed = true;
        }
        else
        {
            completed = thread.Join(timeout);
        }

        if (!completed)
        {
            throw new TimeoutException($"STA test execution exceeded timeout of {timeout}.");
        }

        if (captured is not null)
        {
            ExceptionDispatchInfo.Capture(captured).Throw();
        }
    }

    internal static void RunInSta(Func<Task> action)
    {
        RunInSta(() => action().GetAwaiter().GetResult());
    }

    internal static void RunInStaAndDrain(Action action)
    {
        RunInSta(() =>
        {
            action();
            DoEvents();
        });
    }

    internal static void EnsureAppResources()
    {
        lock (AppInitLock)
        {
            if (WpfApplication.Current is null)
            {
                _ = new WpfApplication
                {
                    ShutdownMode = ShutdownMode.OnExplicitShutdown,
                };
            }

            if (WpfApplication.Current?.Resources["RoundedTextBoxStyle"] is null)
            {
                var dictionary = (ResourceDictionary)WpfApplication.LoadComponent(
                    new Uri("/applanch;component/AppResources.xaml", UriKind.Relative));
                WpfApplication.Current!.Resources.MergedDictionaries.Add(dictionary);
            }
        }
    }

    internal static void ShowOffscreen(Window window)
    {
        ArgumentNullException.ThrowIfNull(window);

        window.WindowStartupLocation = WindowStartupLocation.Manual;
        window.Left = -10000;
        window.Top = -10000;
        window.ShowInTaskbar = false;
        window.ShowActivated = false;
        window.Opacity = 0;
        window.Show();
        DoEvents();
    }

    internal static void DoEvents()
    {
        var frame = new DispatcherFrame();
        Dispatcher.CurrentDispatcher.BeginInvoke(
            DispatcherPriority.Background,
            new Action(() => frame.Continue = false));
        Dispatcher.PushFrame(frame);
    }
}
