using System.IO;
using applanch.Utilities;

namespace applanch.ThemeCreator;

internal static class ThemeCreatorLogger
{
    private const string AppLogFileName = "theme-creator.log";

    private static AppLogger? _logger;

    private static AppLogger Logger
    {
        get
        {
            if (_logger is null)
            {
                var logFilePath = LogFilePath;
                _logger = new AppLogger(logFilePath);
            }

            return _logger;
        }
    }

    internal static string LogDirectoryPath => AppLogger.LogDirectoryPath;

    internal static string LogFilePath => Path.Combine(AppLogger.LogDirectoryPath, AppLogFileName);

    internal static void Info(string message) => Logger.Info(message);

    internal static void Warn(string message) => Logger.Warn(message);

    internal static void Warn(Exception ex, string message) => Logger.Warn(ex, message);

    internal static void Error(Exception ex, string message) => Logger.Error(ex, message);
}
