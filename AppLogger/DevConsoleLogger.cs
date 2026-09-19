namespace file_logger.DevConsoleLogger;

/// <remarks>
/// TODO::[refactor]: зарегать в провайдере и прокидывать логгер через appsettings
/// </remarks>
static class DevConsoleLogger
{
    
    public static void Log(string msg)
    {
        Console.WriteLine($"[LOG]: {msg}");
    }

    public static void LogError(string msg)
    {
        Console.WriteLine($"[ERROR_LOG]: {msg}");
    }

}