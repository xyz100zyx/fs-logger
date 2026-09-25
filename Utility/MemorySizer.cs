namespace file_logger.Utility;

public static class MemorySizer
{
    static readonly uint MegaByte = 1 << 20;
    static readonly uint KiloByte = 1 << 10;

    public static uint GetKbMemoryInBytes(uint sizeKb)
    {
        return KiloByte * sizeKb;
    }

    public static uint GetMbMemoryInBytes(uint sizeMb)
    {
        return MegaByte * sizeMb;
    }
}