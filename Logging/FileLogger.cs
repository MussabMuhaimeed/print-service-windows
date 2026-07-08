namespace PrintService.Windows.Logging;

public sealed class FileLogger
{
    private readonly object _lock = new();
    private readonly string _logPath;
    private readonly List<string> _recent = new();
    private const int MaxRecent = 500;

    public event Action<string>? LogAdded;

    public FileLogger()
    {
        var logDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "PrintService");
        Directory.CreateDirectory(logDir);
        _logPath = Path.Combine(logDir, "print-service.log");
    }

    public string LogFilePath => _logPath;

    public IReadOnlyList<string> GetRecentLogs()
    {
        lock (_lock)
        {
            return _recent.ToList();
        }
    }

    public void Info(string message) => Write("INFO", message);

    public void Warn(string message) => Write("WARN", message);

    public void Error(string message, Exception? ex = null)
    {
        var full = ex is null ? message : $"{message} | {ex.Message}";
        Write("ERROR", full);
    }

    public void LogPrintJob(string printer, int copies, string contentType, long durationMs, bool success, string? error = null)
    {
        var status = success ? "OK" : $"FAILED: {error}";
        Write("PRINT", $"{printer} | copies={copies} | type={contentType} | {durationMs}ms | {status}");
    }

    private void Write(string level, string message)
    {
        var line = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} [{level}] {message}";

        lock (_lock)
        {
            _recent.Add(line);
            if (_recent.Count > MaxRecent)
            {
                _recent.RemoveAt(0);
            }

            try
            {
                File.AppendAllText(_logPath, line + Environment.NewLine);
            }
            catch
            {
                // Ignore log file write failures.
            }
        }

        LogAdded?.Invoke(line);
    }
}
