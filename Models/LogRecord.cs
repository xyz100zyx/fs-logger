using file_logger.Models;

namespace file_logger.Core;


/// <remarks>
/// TODO::[refactor]: заменить этим рекордом прошлую модель эвента в виде класса, чтоб не было аллокаций в куче
/// </remarks>
public readonly ref struct LogRecord
{
    public readonly DateTime Timestamp;
    public readonly FileEventType EventType;
    public readonly ReadOnlySpan<byte> Path;
    public readonly ReadOnlySpan<byte> OldPath;
    public readonly bool IsDirectory;
    public readonly long Size;
    public readonly ReadOnlySpan<byte> Sha256;
    public readonly int Pid;
    public readonly ReadOnlySpan<byte> Process;
    public LogRecord(
        DateTime timestamp,
        FileEventType eventType,
        ReadOnlySpan<byte> path,
        ReadOnlySpan<byte> oldPath,
        bool isDirectory,
        long size,
        ReadOnlySpan<byte> sha256,
        int pid,
        ReadOnlySpan<byte> process)
    {
        Timestamp = timestamp;
        EventType = eventType;
        Path = path;
        OldPath = oldPath;
        IsDirectory = isDirectory;
        Size = size;
        Sha256 = sha256;
        Pid = pid;
        Process = process;
    }
    public bool HasSize => Size != long.MinValue;
    public bool HasSha256 => !Sha256.IsEmpty;
}