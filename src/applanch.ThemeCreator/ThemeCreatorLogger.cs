using System.IO;
using System.Text;

namespace applanch.ThemeCreator;

internal static class ThemeCreatorLogger
{
    private const string LogDirectoryOverrideEnvironmentVariable = "APPLANCH_THEME_CREATOR_LOG_DIRECTORY";
    private const string ProductDirectoryName = "applanch";
    private const string LogFileName = "theme-creator.log";

    private static readonly Lock SyncLock = new();

    internal static string LogFilePath => ResolveLogFilePath();

    internal static void Info(string message)
        => Write("INFO", message);

    internal static void Warn(string message)
        => Write("WARN", message);

    internal static void Warn(Exception ex, string message)
        => WriteException("WARN", ex, message);

    internal static void Error(Exception ex, string message)
        => WriteException("ERROR", ex, message);

    internal static string ResolveLogDirectory(string? overrideDirectory = null)
    {
        if (!string.IsNullOrWhiteSpace(overrideDirectory))
        {
            return overrideDirectory;
        }

        var configuredDirectory = Environment.GetEnvironmentVariable(LogDirectoryOverrideEnvironmentVariable);
        if (!string.IsNullOrWhiteSpace(configuredDirectory))
        {
            return configuredDirectory;
        }

        return Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            ProductDirectoryName,
            "logs");
    }

    internal static string ResolveLogFilePath(string? overrideDirectory = null)
        => Path.Combine(ResolveLogDirectory(overrideDirectory), LogFileName);

    private static void Write(string level, string message)
    {
        try
        {
            var logPath = ResolveLogFilePath();
            Directory.CreateDirectory(Path.GetDirectoryName(logPath)!);

            lock (SyncLock)
            {
                File.AppendAllText(logPath, $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] [{level}] {message}{Environment.NewLine}", Encoding.UTF8);
            }
        }
        catch
        {
            // Logging must never crash the tool.
        }
    }

    private static void WriteException(string level, Exception ex, string message)
    {
        var details = new StringBuilder()
            .Append(message)
            .Append(" | ")
            .Append(ex.GetType().Name)
            .Append(": ")
            .Append(ex.Message);

        if (!string.IsNullOrWhiteSpace(ex.StackTrace))
        {
            details.Append(" | StackTrace: ").Append(ex.StackTrace);
        }

        Write(level, details.ToString());
    }
}