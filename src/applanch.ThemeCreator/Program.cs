using System.IO;
using System.Runtime.InteropServices;

namespace applanch.ThemeCreator;

internal static partial class Program
{
    private const int AttachParentProcess = -1;

    [LibraryImport("kernel32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool AttachConsole(int dwProcessId);

    [STAThread]
    private static int Main(string[] args)
    {
        if (args.Length > 0)
        {
            AttachConsole(AttachParentProcess);
            Console.SetOut(new StreamWriter(Console.OpenStandardOutput()) { AutoFlush = true });
            Console.SetError(new StreamWriter(Console.OpenStandardError()) { AutoFlush = true });
        }

        return ThemeCreatorApp.Run(args, Console.Out, Console.Error);
    }
}
