using System.Runtime.ExceptionServices;
using System.Runtime.Versioning;
using System.Windows;
using System.Windows.Threading;
using WpfApplication = System.Windows.Application;

namespace applanch.Tests.Support;

[SupportedOSPlatform("windows")]
public static class WpfTestHost
{
    public static void RunInSta(Action action) => RunInSta(action, Timeout.InfiniteTimeSpan);

    public static void RunInSta(Action action, TimeSpan timeout)
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

    public static void RunInSta(Func<Task> action) => RunInSta(() => action().GetAwaiter().GetResult());

    public static void RunInStaAndDrain(Action action) => RunInSta(() =>
    {
        action();
        DoEvents();
    });

    public static void EnsureApplication()
    {
        if (WpfApplication.Current is null)
        {
            _ = new WpfApplication
            {
                ShutdownMode = ShutdownMode.OnExplicitShutdown,
            };
        }
    }

    public static void ShowOffscreen(Window window)
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

    public static void DoEvents()
    {
        var frame = new DispatcherFrame();
        Dispatcher.CurrentDispatcher.BeginInvoke(
            DispatcherPriority.Background,
            new Action(() => frame.Continue = false));
        Dispatcher.PushFrame(frame);
    }
}
