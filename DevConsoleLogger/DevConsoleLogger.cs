namespace file_logger.DevConsoleLogger;

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