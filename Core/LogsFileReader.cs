using System.Buffers;
using file_logger.Core;

public sealed class LogsFileReader
{
    private string _filepath;
    private uint _bufferSize;

    private int _offset;
    private int _total;

    private CancellationTokenSource _cts;

    public LogsFileReader(string filepath, uint bufferSize = 2 << 20)
    {
        _filepath = filepath;
        _bufferSize = bufferSize;
        _cts = new();
        _offset = 0;
    }

    public void Read(HandleLog onLogProcessed)
    {

        _cts = new();

        using FileStream fs = new(_filepath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete, 1 << 16, FileOptions.SequentialScan);
        var rentedBuffer = ArrayPool<byte>.Shared.Rent((int)_bufferSize);

        if(_total >= fs.Length)
        {
            DevConsoleLogger.Log("File was readed full. Waiting for new changes in file...");
            return;
        }

        try
        {
            _offset = 0;
            while (true)
            {
                if (_cts.Token.IsCancellationRequested || rentedBuffer is null) break;

                int readed = fs.Read(rentedBuffer, _offset, rentedBuffer.Length - _offset);

                if (readed == 0 && _offset == 0)  break;

                _total = readed + _offset;
                var span = rentedBuffer.AsSpan<byte>(0, _total);
                int start = 0;

                while (true)
                {
                    if (_cts.Token.IsCancellationRequested) break;

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
                        DevConsoleLogger.LogError("Unable to read log. JsonLineParser.TryParseLine returns false");
                    }
                    start = lineEnd + 1;
                }
                int remaining = _total - start;
                if (remaining > 0)
                {
                    if (remaining >= rentedBuffer.Length)
                    {
                        throw new InvalidOperationException(
                            "Строка превышает размер буфера. Увеличьте bufferSize.");
                    }
                    Buffer.BlockCopy(rentedBuffer, start, rentedBuffer, 0, remaining);
                }
                _offset = remaining;

                if (readed == 0)
                    break;
            }
            if (_offset > 0)
            {
                ReadOnlySpan<byte> lastLine = rentedBuffer.AsSpan(0, _offset);
                if (JsonLineParser.TryParseLine(lastLine, out var record))
                    onLogProcessed(in record);
            }
        }
        catch (Exception ex)
        {
            DevConsoleLogger.LogError($"Error during parsing jsonl (inside JsonFileReader.Read) {ex.Message}");
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(rentedBuffer, true);
        }
    }

    public void AbortReading()
    {
        DevConsoleLogger.Log("Abort log file reader");
        _cts.Cancel();
    }

}

public delegate void HandleLog(in LogRecord logRecord);