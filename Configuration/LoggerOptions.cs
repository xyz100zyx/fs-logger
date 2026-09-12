public sealed class LoggerOptions
{
    public const string SectionName = "Logger";

    /// <summary>
    /// каталог для наблюдения за измененими
    /// </summary>
    public string WatchableDirectoryPath { get; set; } = Directory.GetCurrentDirectory();

    /// <summary>
    /// папка в которую будет ложиться логи
    /// </summary>
    public string FolderWithLogs { get; set; } = "logs";

    /// <summary>
    ///имя файла с логами
    /// </summary>
    public string LogsFileName { get; set; } = "fslogs";

    /// <summary>
    /// максимальный размер файла логов
    /// </summary>
    public long MaxObjectSizeBytes = 1024 * 1024 * 10;

    /// <summary>
    /// delta t, по которому определяется - event дублирующийся или нет, нужно производить дедупликацию события или нет
    /// </summary>
    public long DeduplicationWindowsMs = 300;

    public bool ComputeHash { get; set; } = true;

    /// <summary>
    /// если размер больше 50МБ то для него не считаем хеш
    /// </summary>
    public long MaxHashFileSizeBytes { get; set; } = 50 * 1024 * 1024;

    public bool IncludeSubdirectories { get; set; } = true;
}