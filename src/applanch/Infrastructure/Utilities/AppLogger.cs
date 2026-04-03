using System.IO;
using System.Runtime.CompilerServices;

namespace applanch.Infrastructure.Utilities;

internal sealed class AppLogger : IDisposable
{
    private static readonly string LogDirectory = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "applanch");

    private static readonly string LogFilePath = Path.Combine(LogDirectory, "app.log");
    private static readonly long MaxLogSize = 1024 * 1024; // 1 MB

    internal static string LogDirectoryPath => LogDirectory;
    internal static string LogFilePathValue => LogFilePath;

    private readonly Lock _lock = new();
    private StreamWriter? _writer;

    public static AppLogger Instance { get; } = new();

    private AppLogger()
    {
        Directory.CreateDirectory(LogDirectory);
        RotateIfNeeded();
        _writer = CreateWriter();
        _writer.WriteLine();
        _writer.WriteLine($"===== App started at {DateTime.Now:yyyy-MM-dd HH:mm:ss} =====");
    }

    public void Info(string message, [CallerMemberName] string? caller = null, [CallerFilePath] string? file = null)
    {
        var source = FormatSource(caller, file);
        Write($"[{DateTime.Now:HH:mm:ss.fff}] [INFO] [{source}] {message}");
    }

    public void Warn(string message, [CallerMemberName] string? caller = null, [CallerFilePath] string? file = null)
    {
        var source = FormatSource(caller, file);
        Write($"[{DateTime.Now:HH:mm:ss.fff}] [WARN] [{source}] {message}");
    }

    public void Error(Exception ex, string? message = null, [CallerMemberName] string? caller = null, [CallerFilePath] string? file = null)
    {
        var source = FormatSource(caller, file);
        var prefix = message is not null ? $"{message} — " : "";
        Write($"[{DateTime.Now:HH:mm:ss.fff}] [ERROR] [{source}] {prefix}{ex.GetType().Name}: {ex.Message}");
        if (ex.InnerException is { } inner)
        {
            Write($"  Inner: {inner.GetType().Name}: {inner.Message}");
        }
        Write($"  StackTrace: {ex.StackTrace}");
    }

    public void Dispose()
    {
        lock (_lock)
        {
            _writer?.Dispose();
            _writer = null;
        }
    }

    private void Write(string line)
    {
        try
        {
            lock (_lock)
            {
                _writer?.WriteLine(line);
            }
        }
        catch
        {
            // Logging must never crash the app.
        }
    }

    private static StreamWriter CreateWriter()
    {
        var stream = new FileStream(LogFilePath, FileMode.Append, FileAccess.Write, FileShare.ReadWrite | FileShare.Delete);
        return new StreamWriter(stream) { AutoFlush = true };
    }

    private void RotateIfNeeded()
    {
        if (!File.Exists(LogFilePath))
        {
            return;
        }

        try
        {
            if (new FileInfo(LogFilePath).Length > MaxLogSize)
            {
                var backupPath = LogFilePath + ".old";
                File.Delete(backupPath);
                File.Move(LogFilePath, backupPath);
            }
        }
        catch
        {
            // Best-effort rotation.
        }
    }

    private static string FormatSource(string? caller, string? file)
    {
        var fileName = file is not null ? Path.GetFileNameWithoutExtension(file) : null;
        return fileName is not null ? $"{fileName}.{caller}" : caller ?? "Unknown";
    }
}

