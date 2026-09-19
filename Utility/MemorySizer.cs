namespace file_logger.Utility;

public static class MemorySize
{
    static readonly long MegaByte = 1 << 20;
    static readonly long KiloByte = 1 << 10;

    public static long GetKbMemoryInBytes(long sizeKb)
    {
        return KiloByte * sizeKb;
    }
    public static long GetMbMemoryInBytes(long sizeMb)
    {
        return MegaByte * sizeMb;
    }
}