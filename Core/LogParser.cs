using System.Text.Json;
using file_logger.Models;

namespace file_logger.Core;


public static class LogParser
{

    private static readonly JsonReaderOptions _jsonReaderOptions = new()
    {
        AllowTrailingCommas = false,
        CommentHandling = JsonCommentHandling.Disallow
    };
}