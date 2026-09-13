using System.Formats.Asn1;
using System.Text;
using DevLogger = file_logger.DevConsoleLogger.DevConsoleLogger;

namespace file_logger.Core;

public sealed class LogRotator : IDisposable
{
    private readonly LoggerOptions _loggerOpts;

    private readonly object _lock = new();

    private StreamWriter? _streamWriter;

    private string _currentFilePath;

    private DateOnly _currentDate;

    public LogRotator(LoggerOptions opts)
    {
        _loggerOpts = opts;
        CreateLogsDirectory();
    }

    public void CreateLogsDirectory()
    {
        Directory.CreateDirectory(_loggerOpts.FolderWithLogs);
    }

    public void WriteLogLine(string logLine)
    {

        try
        {
            lock (_lock)
            {
                EnsureWrite();

                _streamWriter!.WriteLine(logLine);
                _streamWriter.Flush();

                if (_streamWriter.BaseStream.Length > _loggerOpts.MaxObjectSizeBytes)
                {
                    Rotate(force: true);
                }
            }
        }
        catch (Exception ex)
        {
            DevLogger.LogError($"Error on write log line into logs file. Method WriteLogLine.\nErrMsg={ex.Message}");
        }

    }

    private void EnsureWrite()
    {
        var todayDateOnly = DateOnly.FromDateTime(DateTime.Now);


        if (_streamWriter != null && todayDateOnly != _currentDate)
        {

            Rotate(force: true);

        }

        if (_streamWriter != null) return;

        _currentDate = todayDateOnly;
        _currentFilePath = BuildFileName(todayDateOnly);

        var fileStream = new FileStream(_currentFilePath, FileMode.Append, FileAccess.Read, FileShare.Read);
        _streamWriter = new StreamWriter(fileStream, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false))
        {
            AutoFlush = true
        };

    }

    private string BuildFileName(DateOnly date)
    {
        string name = $"{date:yyyy-MM-dd}.jsonl";
        return Path.Combine(_loggerOpts.FolderWithLogs, name);
    }

    private void Rotate(bool force)
    {
        if (_streamWriter == null) return;

        CleanupWriter();

        if (!force) return;

        string archiveBase = Path.Combine(
        _loggerOpts.FolderWithLogs,
    $"archive-{_currentDate:yyyy-MM-dd}");

        int n = 1;
        string archivePath;
        do
        {
            archivePath = $"{archiveBase}.{n:000}.jsonl";
            n++;
        } while (File.Exists(archivePath));

        File.Move(_currentFilePath, archivePath);

    }

    private void CleanupWriter()
    {
        _streamWriter?.Flush();
        _streamWriter?.Dispose();
        _streamWriter = null;
    }

    public void Dispose()
    {
        lock (_lock)
        {
            CleanupWriter();
        }
    }
}