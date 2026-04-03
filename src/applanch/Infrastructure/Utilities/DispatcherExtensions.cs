using System.Windows.Threading;

namespace applanch.Infrastructure.Utilities;

internal static class DispatcherExtensions
{
    internal static void InvokeIfRequired(this Dispatcher dispatcher, Action action)
    {
        if (dispatcher.CheckAccess())
        {
            action();
            return;
        }

        dispatcher.Invoke(action);
    }
}
