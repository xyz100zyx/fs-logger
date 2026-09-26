using file_logger.Core;
using file_logger.Utility;
using Interval = System.Timers.Timer;

namespace file_logger.Core;

class LogsReaderSessionManager : IDisposable
{

    private string _filepath;

    private LogsFileReader _currentFileReader { get; init; }

    private SessionInteval _sessionRotatorInterval;

    public LogsReaderSessionManager(string filepath)
    {
        _filepath = filepath;
        _sessionRotatorInterval = new(OnSessionRotate);
        _currentFileReader = new(_filepath, MemorySizer.GetMbMemoryInBytes(4));
    }

    public void RunSession()
    {
        ReOpenReadingFile();
        _sessionRotatorInterval.StartInterval();
    }

    private void ReOpenReadingFile()
    {
        _currentFileReader.Read((in LogRecord log) =>
       {
           DevConsoleLogger.Log(log.ToString());
       });
    }

    private void OnSessionRotate()
    {
        _currentFileReader.AbortReading();
        ReOpenReadingFile();
    }

    public void Dispose()
    {
        _sessionRotatorInterval.Dispose();
        _currentFileReader.AbortReading();
    }

}


class SessionInteval : IDisposable
{

    private Interval _rotationInterval;

    public SessionInteval(HandleInterval intervalHandler)
    {
        _rotationInterval = new(5_000)
        {
            AutoReset = true
        };
        _rotationInterval.Elapsed += (_, __) =>
        {
            intervalHandler();
        };
    }

    public void StartInterval()
    {
        _rotationInterval.Enabled = true;
    }

    public void StopInterval()
    {
        _rotationInterval.Enabled = false;
    }

    public void Dispose()
    {
        StopInterval();
        _rotationInterval.Dispose();
    }

}

delegate void HandleInterval();