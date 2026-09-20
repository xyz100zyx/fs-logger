using System.Buffers;
using file_logger.Core;

public sealed class JsonFileReader
{
    private string _filepath;
    private uint _bufferSize;
    public JsonFileReader(string filepath, uint bufferSize = 2 << 20)
    {
        _filepath = filepath;
        _bufferSize = bufferSize;
    }

    public void Read(HandleLog onLogProcessed)
    {
        using FileStream fs = new(_filepath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete, 1 << 16, FileOptions.SequentialScan);
        var rentedBuffer = ArrayPool<byte>.Shared.Rent((int)_bufferSize);

        try
        {
            int offset = 0;

            while (true)
            {
                int readed = fs.Read(rentedBuffer, offset, rentedBuffer.Length - offset);
                if (readed == 0 && offset == 0) break;
                int total = readed + offset;
                var span = rentedBuffer.AsSpan<byte>(0, total);
                int start = 0;
                while (true)
                {
                    int newLineSymbol = span[start..].IndexOf((byte)'\n');
                    if (newLineSymbol < 0)
                        break;
                    int lineEnd = start + newLineSymbol;
                    ReadOnlySpan<byte> line = span[start..lineEnd];
                    if (JsonLineParser.TryParseLine(line, out var record))
                    {
                        onLogProcessed(in record);
                    }
                    else
                    {
                        // TODO::bussiness: если не получилось распарсить, кинуть лог в клик на мануальный анализ
                    }
                    start = lineEnd + 1;
                }
                int remaining = total - start;
                if (remaining > 0)
                {
                    if (remaining >= rentedBuffer.Length)
                    {
                        throw new InvalidOperationException(
                            "Строка превышает размер буфера. Увеличьте bufferSize.");
                    }
                    Buffer.BlockCopy(rentedBuffer, start, rentedBuffer, 0, remaining);
                }
                offset = remaining;

                if (readed == 0)
                    break;
            }
            if (offset > 0)
            {
                ReadOnlySpan<byte> lastLine = rentedBuffer.AsSpan(0, offset);
                if (JsonLineParser.TryParseLine(lastLine, out var record))
                    onLogProcessed(in record);
            }
        }
        catch (Exception ex)
        {
            // TODO::bussiness: обработка
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(rentedBuffer);
        }
    }

}

public delegate void HandleLog(in LogRecord logRecord);