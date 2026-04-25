using System.Runtime.CompilerServices;
using System.Diagnostics;

namespace applanch.Utilities;

public sealed class AppLogger : IDisposable
{
    private const string LogDirectoryOverrideEnvironmentVariable = "APPLANCH_LOG_DIRECTORY";
    private static readonly long MaxLogSize = 1024 * 1024; // 1 MB

    private static readonly string DefaultLogDirectory = ResolveLogDirectory();
    private static readonly string DefaultLogFilePath = Path.Combine(DefaultLogDirectory, "app.log");

    public static string LogDirectoryPath => DefaultLogDirectory;
    public static string LogFilePathValue => DefaultLogFilePath;

    /// <summary>Gets the full path of the log file this instance writes to.</summary>
    public string LogFilePath => _logFilePath;

    private readonly Lock _lock = new();
    private readonly string _logFilePath;
    private StreamWriter? _writer;

    public static AppLogger Instance { get; } = new();

    /// <summary>
    /// Creates a new <see cref="AppLogger"/> that writes to a file named
    /// <paramref name="logFileName"/> inside <see cref="LogDirectoryPath"/>.
    /// </summary>
    public static AppLogger CreateNamed(string logFileName) =>
        new(Path.Combine(LogDirectoryPath, logFileName));

    private AppLogger()
        : this(DefaultLogFilePath)
    {
        _writer!.WriteLine();
        _writer.WriteLine($"===== App started at {DateTime.Now:yyyy-MM-dd HH:mm:ss} =====");
    }

    public AppLogger(string logFilePath)
    {
        _logFilePath = logFilePath ?? throw new ArgumentNullException(nameof(logFilePath));
        var logDirectory = Path.GetDirectoryName(_logFilePath);
        if (logDirectory is not null)
        {
            Directory.CreateDirectory(logDirectory);
        }

        RotateIfNeeded();
        _writer = CreateWriter();
    }

    [Conditional("DEBUG")]
    public void Debug(string message, [CallerMemberName] string? caller = null, [CallerFilePath] string? file = null)
    {
        var source = FormatSource(caller, file);
        Write($"[{DateTime.Now:HH:mm:ss.fff}] [DEBUG] [{source}] {message}");
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

    public void Warn(Exception ex, string? message = null, [CallerMemberName] string? caller = null, [CallerFilePath] string? file = null)
    {
        var source = FormatSource(caller, file);
        WriteException("WARN", source, ex, message);
    }

    public void Error(Exception ex, string? message = null, [CallerMemberName] string? caller = null, [CallerFilePath] string? file = null)
    {
        var source = FormatSource(caller, file);
        WriteException("ERROR", source, ex, message);
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

    private StreamWriter CreateWriter()
    {
        var stream = new FileStream(_logFilePath, FileMode.Append, FileAccess.Write, FileShare.ReadWrite | FileShare.Delete);
        return new StreamWriter(stream) { AutoFlush = true };
    }

    private void RotateIfNeeded()
    {
        if (!File.Exists(_logFilePath))
        {
            return;
        }

        try
        {
            if (new FileInfo(_logFilePath).Length > MaxLogSize)
            {
                var backupPath = _logFilePath + ".old";
                File.Delete(backupPath);
                File.Move(_logFilePath, backupPath);
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

    private void WriteException(string level, string source, Exception ex, string? message)
    {
        var prefix = message is not null ? $"{message} — " : "";
        Write($"[{DateTime.Now:HH:mm:ss.fff}] [{level}] [{source}] {prefix}{ex.GetType().Name}: {ex.Message}");
        if (ex.InnerException is { } inner)
        {
            Write($"  Inner: {inner.GetType().Name}: {inner.Message}");
        }

        Write($"  StackTrace: {ex.StackTrace}");
    }

    internal static string ResolveLogDirectory(string? overrideDirectory = null, string? processName = null)
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

        if (IsLikelyTestProcess(processName))
        {
            return Path.Combine(Path.GetTempPath(), "applanch-test-logs");
        }

        return AppDataPaths.LocalApplicationDataDirectory;
    }

    internal static bool IsLikelyTestProcess(string? processName = null)
    {
        var effectiveProcessName = processName;
        if (string.IsNullOrWhiteSpace(effectiveProcessName))
        {
            effectiveProcessName = Process.GetCurrentProcess().ProcessName;
        }

        return !string.IsNullOrWhiteSpace(effectiveProcessName) &&
            effectiveProcessName.IndexOf("testhost", StringComparison.OrdinalIgnoreCase) >= 0;
    }
}


