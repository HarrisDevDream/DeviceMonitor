using System;
using System.Diagnostics;
using System.IO;
using System.Text;

namespace DeviceMonitor.Services;

/// <summary>
/// Logging service for debugging and error tracking
/// </summary>
public class LogService
{
    private static LogService? _instance;
    private static readonly object _lock = new();
    private readonly string _logFilePath;
    private readonly string _logDirectory;

    public static LogService Instance
    {
        get
        {
            if (_instance == null)
            {
                lock (_lock)
                {
                    _instance ??= new LogService();
                }
            }
            return _instance;
        }
    }

    private LogService()
    {
        // Create log directory in user's AppData
        _logDirectory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "DeviceMonitor",
            "Logs"
        );

        Directory.CreateDirectory(_logDirectory);

        // Create one log file per day
        var logFileName = $"DeviceMonitor_{DateTime.Now:yyyyMMdd}.log";
        _logFilePath = Path.Combine(_logDirectory, logFileName);

        // Log system info on startup
        LogSystemInfo();
    }

    /// <summary>
    /// Get log file path (for user access)
    /// </summary>
    public string LogFilePath => _logFilePath;

    /// <summary>
    /// Get log directory path
    /// </summary>
    public string LogDirectory => _logDirectory;

    /// <summary>
    /// Log system information
    /// </summary>
    private void LogSystemInfo()
    {
        try
        {
            var info = new StringBuilder();
            info.AppendLine("=".PadRight(80, '='));
            info.AppendLine($"DeviceMonitor Started at {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            info.AppendLine("=".PadRight(80, '='));
            info.AppendLine($"OS: {Environment.OSVersion}");
            info.AppendLine($"64-bit OS: {Environment.Is64BitOperatingSystem}");
            info.AppendLine($"64-bit Process: {Environment.Is64BitProcess}");
            info.AppendLine($".NET Version: {Environment.Version}");
            info.AppendLine($"Machine Name: {Environment.MachineName}");
            info.AppendLine($"User Name: {Environment.UserName}");
            info.AppendLine($"Is Administrator: {IsAdministrator()}");
            info.AppendLine($"Working Directory: {Environment.CurrentDirectory}");
            info.AppendLine($"Log Directory: {_logDirectory}");
            info.AppendLine("=".PadRight(80, '='));

            File.AppendAllText(_logFilePath, info.ToString());
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Failed to log system info: {ex.Message}");
        }
    }

    /// <summary>
    /// Check if running with administrator privileges
    /// </summary>
    private bool IsAdministrator()
    {
        try
        {
            var identity = System.Security.Principal.WindowsIdentity.GetCurrent();
            var principal = new System.Security.Principal.WindowsPrincipal(identity);
            return principal.IsInRole(System.Security.Principal.WindowsBuiltInRole.Administrator);
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Log info level message
    /// </summary>
    public void Info(string message)
    {
        Log("INFO", message);
    }

    /// <summary>
    /// Log warning level message
    /// </summary>
    public void Warning(string message)
    {
        Log("WARN", message);
    }

    /// <summary>
    /// Log error level message
    /// </summary>
    public void Error(string message, Exception? ex = null)
    {
        var fullMessage = message;
        if (ex != null)
        {
            fullMessage += $"\nException: {ex.GetType().Name}\nMessage: {ex.Message}\nStackTrace:\n{ex.StackTrace}";
            if (ex.InnerException != null)
            {
                fullMessage += $"\nInner Exception: {ex.InnerException.Message}";
            }
        }
        Log("ERROR", fullMessage);

        // Also write to Windows Event Log
        WriteToEventLog(message, ex);
    }

    /// <summary>
    /// Log debug level message (Debug mode only)
    /// </summary>
    public void LogDebug(string message)
    {
#if DEBUG
        Log("DEBUG", message);
#endif
    }

    /// <summary>
    /// Core logging method
    /// </summary>
    private void Log(string level, string message)
    {
        try
        {
            var timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
            var threadId = Environment.CurrentManagedThreadId;
            var logEntry = $"[{timestamp}] [{level}] [Thread-{threadId}] {message}\n";

            // Write to file
            File.AppendAllText(_logFilePath, logEntry);

            // Also output to Debug Console (visible during development)
            System.Diagnostics.Debug.WriteLine($"[{level}] {message}");
        }
        catch (Exception ex)
        {
            // If logging fails, at least output to Debug
            System.Diagnostics.Debug.WriteLine($"Logging failed: {ex.Message}");
        }
    }

    /// <summary>
    /// Write to Windows Event Viewer (error level only)
    /// </summary>
    private void WriteToEventLog(string message, Exception? ex)
    {
        try
        {
            const string source = "DeviceMonitor";
            const string log = "Application";

            // Check if Event Source exists, create if not
            if (!EventLog.SourceExists(source))
            {
                // Requires administrator privilege to create
                EventLog.CreateEventSource(source, log);
            }

            var eventMessage = message;
            if (ex != null)
            {
                eventMessage += $"\n\nException Details:\n{ex}";
            }

            // Write to Event Log
            EventLog.WriteEntry(source, eventMessage, EventLogEntryType.Error);
        }
        catch (Exception)
        {
            // Event Log write failure is acceptable (may lack admin privileges)
            // File logging is the primary mechanism
        }
    }

    /// <summary>
    /// Clean old log files (keep last 30 days)
    /// </summary>
    public void CleanOldLogs(int daysToKeep = 30)
    {
        try
        {
            var cutoffDate = DateTime.Now.AddDays(-daysToKeep);
            var files = Directory.GetFiles(_logDirectory, "*.log");

            foreach (var file in files)
            {
                var fileInfo = new FileInfo(file);
                if (fileInfo.CreationTime < cutoffDate)
                {
                    fileInfo.Delete();
                    Info($"Deleted old log file: {fileInfo.Name}");
                }
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Failed to clean old logs: {ex.Message}");
        }
    }

    /// <summary>
    /// Get recent log content (for displaying to user)
    /// </summary>
    public string GetRecentLogs(int lines = 100)
    {
        try
        {
            if (!File.Exists(_logFilePath))
                return "No logs available";

            var allLines = File.ReadAllLines(_logFilePath);
            var recentLines = allLines.Length > lines
                ? allLines[^lines..]  // Get last N lines
                : allLines;

            return string.Join(Environment.NewLine, recentLines);
        }
        catch (Exception ex)
        {
            return $"Failed to read logs: {ex.Message}";
        }
    }

    /// <summary>
    /// Open log folder in File Explorer
    /// </summary>
    public void OpenLogFolder()
    {
        try
        {
            Process.Start("explorer.exe", _logDirectory);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Failed to open log folder: {ex.Message}");
        }
    }
}
