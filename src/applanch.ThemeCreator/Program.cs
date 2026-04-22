namespace applanch.ThemeCreator;

internal static class Program
{
    [STAThread]
    private static int Main(string[] args)
        => ThemeCreatorApp.Run(args, Console.Out, Console.Error);
}
