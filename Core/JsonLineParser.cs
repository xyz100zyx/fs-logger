using System.Buffers.Text;
using System.Runtime.CompilerServices;
using System.Text.Json;
using file_logger.Models;

namespace file_logger.Core;

public static class JsonLineParser
{
    public static bool TryParseLine(ReadOnlySpan<byte> line, out LogRecord record)
    {
        record = default;

        if (!line.IsEmpty && line[^1] == (byte)'\r')
            line = line[..^1];

        if (line.IsEmpty)
            return false;

        var reader = new Utf8JsonReader(line, isFinalBlock: true, state: default);

        DateTime ts = default;
        FileEventType type = default;
        ReadOnlySpan<byte> path = default;
        ReadOnlySpan<byte> oldPath = default;
        bool isDir = false;
        long size = long.MinValue;
        ReadOnlySpan<byte> sha256 = default;
        int pid = default;
        ReadOnlySpan<byte> proc = default;

        if (!reader.Read() || reader.TokenType != JsonTokenType.StartObject)
            return false;

        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.EndObject)
            {
                record = new LogRecord(ts, type, path, oldPath, isDir, size, sha256, pid, proc);
                return true;
            }
            if (reader.TokenType != JsonTokenType.PropertyName) return false;
            var propertyName = reader.ValueSpan;
            // после получения типа токена читаем значение;
            if (!reader.Read()) return false;
            switch (propertyName.Length)
            {
                case 2:
                    if (propertyName[0] == (byte)'t' && propertyName[1] == (byte)'s')
                    {
                        if (reader.TokenType == JsonTokenType.String && TryParseIsoTimestamp(reader.ValueSpan, out ts))
                        {
                            continue;
                        }
                        return false;
                    }
                    break;
                case 3:
                    if (propertyName[0] == (byte)'p' && propertyName[1] == (byte)'i' && propertyName[2] == (byte)'d')
                    {
                        if (reader.TokenType == JsonTokenType.Number && reader.TryGetInt32(out pid))
                        {
                            continue;
                        }
                        return false;
                    }
                    break;
                case 4:
                    if (propertyName.SequenceEqual("type"u8))
                    {
                        if (reader.TokenType == JsonTokenType.String)
                        {
                            GetEventType(reader.ValueSpan, out type);
                            continue;
                        }
                        return false;
                    }

                    if (propertyName.SequenceEqual("path"u8))
                    {
                        if (reader.TokenType == JsonTokenType.String)
                        {
                            path = reader.ValueSpan;
                            continue;
                        }
                        return false;
                    }
                    if (propertyName.SequenceEqual("size"u8))
                    {
                        if (reader.TokenType == JsonTokenType.String)
                        {
                            reader.TryGetInt64(out size);
                            continue;
                        }
                        if (reader.TokenType == JsonTokenType.Number)
                        {
                            reader.TryGetInt64(out size);
                            continue;
                        }
                        if (reader.TokenType == JsonTokenType.Null)
                        {
                            size = long.MinValue;
                            continue;
                        }
                        return false;
                    }
                    break;
                case 5:
                    if (propertyName.SequenceEqual("isDir"u8))
                    {
                        if (reader.TokenType == JsonTokenType.True) { isDir = true; continue; }
                        if (reader.TokenType == JsonTokenType.False) { isDir = false; continue; }
                        return false;
                    }
                    break;
                case 6:
                    if (propertyName.SequenceEqual("sha256"u8))
                    {
                        if (reader.TokenType == JsonTokenType.String)
                        {
                            sha256 = reader.ValueSpan;
                            continue;
                        }
                        if (reader.TokenType == JsonTokenType.Null)
                        {
                            sha256 = default;
                            continue;
                        }
                        return false;
                    }
                    break;
                case 7:
                    if (propertyName.SequenceEqual("oldPath"u8))
                    {
                        if (reader.TokenType == JsonTokenType.String)
                        {
                            oldPath = reader.ValueSpan;
                            continue;
                        }
                        if (reader.TokenType == JsonTokenType.Null)
                        {
                            oldPath = default;
                            continue;
                        }
                        return false;
                    }
                    break;
                default:
                    DevConsoleLogger.LogError("Is reached unhandled jsonl log component");
                    break;
            }
            if (reader.TokenType == JsonTokenType.StartObject ||
                reader.TokenType == JsonTokenType.StartArray)
            {
                reader.Skip();
            }
        }
        return true;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool TryParseIsoTimestamp(ReadOnlySpan<byte> utf8, out DateTime ts)
    {
        if (Utf8Parser.TryParse(utf8, out DateTime parsed, out int bytesConsumed, 'O') && bytesConsumed == utf8.Length)
        {
            ts = parsed;
            return true;
        }
        // TODO::[bussiness]: придумать fallback на случай если формат даты поменяется;
        ts = default;
        return false;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void GetEventType(ReadOnlySpan<byte> parsedType, out FileEventType type)
    {
        type = default;
        FileEventType? calculated = parsedType.Length switch
        {
            7 when parsedType.SequenceEqual("Created"u8) => FileEventType.Created,
            7 when parsedType.SequenceEqual("Changed"u8) => FileEventType.Changed,
            7 when parsedType.SequenceEqual("Renamed"u8) => FileEventType.Renamed,
            7 when parsedType.SequenceEqual("Deleted"u8) => FileEventType.Deleted,
            7 when parsedType.SequenceEqual("Updated"u8) => FileEventType.Updated,
            _ => null
        };
        if (calculated is not null)
        {
            type = calculated.Value;
        }
    }
}