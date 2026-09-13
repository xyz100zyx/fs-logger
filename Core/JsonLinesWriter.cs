using System.Text;
using System.Text.Json;
using file_logger.Models;

namespace file_logger.Core;

/// <summary>
/// Биекция FileEvent в строку JSON
/// </summary>
public sealed class JsonLinesWriter
{
    private static readonly JsonSerializerOptions _jsonSerializerOptions = new()
    {
        Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        WriteIndented = false
    };

    public string Serialize(FileEvent evt)
    {

        var payload = new
        {
            ts = evt.Timestamp.UtcDateTime.ToString("o"), // ISO-8601 UTC
            type = evt.EventType.ToString().ToLowerInvariant(),
            path = evt.FullPathToObject,
            oldPath = evt.OldFullPathToObject,
            isDir = evt.IsDirectory,
            size = evt.SizeOfObject,
            sha256 = evt.Sha256,
            pid = evt.ProcessId,
            proc = evt.ProcessName,
        };

        return JsonSerializer.Serialize(payload, _jsonSerializerOptions);
    }
}