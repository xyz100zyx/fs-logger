namespace file_logger.Models;

public enum FileEventType
{
    Created,
    Chnaged,
    Deleted,
    Updated
}

/// <summary>
/// Одно зафиксированное событие файловой системы.
/// Это то, что в итоге попадёт в лог.
/// </summary>
public sealed class FileEvent
{
    public DateTimeOffset Timestamp {get;init;} = DateTimeOffset.UtcNow;

    public required FileEventType EventType {get; init;}

    /// <summary>
    /// полный путь к файлу или папке (объекту, если обобщенно)
    /// </summary>
    public required string FullPathToObject {get; init;}

    /// <summary>
    /// поле для того случая, если изменится наименование объекта в каталоге
    /// </summary>
    public string? OldFullPathToObject {get; init;}

    /// <summary>
    /// true, если это директория
    /// </summary>
    public bool IsDirectory { get; init; }

    /// <summary>       
    /// размер объекта в байтах
    /// </summary>
    public long SizeOfObject {get; init;}

    public string? Sha256 { get; init; }

    public int? ProcessId { get; init; }
    public string? ProcessName { get; init; }

    public string BuildKey() => $"{this.EventType}|{this.FullPathToObject}";
}
