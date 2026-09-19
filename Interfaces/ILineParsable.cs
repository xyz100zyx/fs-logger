namespace file_logger.Interfaces;

public interface IFileParser : IDisposable
{
    public void StartReading();
    public void StopReading();
}