using applanch.Utilities;

namespace applanch.ThemeCreator;

internal static class ThemeCreatorLogger
{
    private const string AppLogFileName = "theme-creator.log";

    private static readonly AppLogger Logger = AppLogger.CreateNamed(AppLogFileName);

    internal static string LogDirectoryPath => AppLogger.LogDirectoryPath;

    internal static string LogFilePath => Logger.LogFilePath;

    internal static void Info(string message) => Logger.Info(message);

    internal static void Warn(string message) => Logger.Warn(message);

    internal static void Warn(Exception ex, string message) => Logger.Warn(ex, message);

    internal static void Error(Exception ex, string message) => Logger.Error(ex, message);
}
