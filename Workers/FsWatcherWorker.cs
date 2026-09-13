using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using file_logger.Core;
using System.Threading.Channels;
using file_logger.Models;
using Microsoft.Extensions.Options;

namespace file_logger.Workers;

/// <summary>
/// FsWatcherWorker занимется наблюдаемостью за каталогом и пушит логи;
/// </summary>
public sealed class FsWatcherWorker : BackgroundService
{

    private readonly LoggerOptions _loggerOptions;
    private readonly ILogger<FsWatcherWorker> _logger;

    private readonly EventDeduplicator _dedup;
    private readonly FileMetaDataCollector _metadataCollector;
    private readonly JsonLinesWriter _serializer;

    private readonly Channel<FileEvent> _channel;

    private FileSystemWatcher? _watcher;

    public FsWatcherWorker(IOptions<LoggerOptions> options, ILogger<FsWatcherWorker> logger)
    {
        _loggerOptions = options.Value;
        _logger = logger;

        _dedup = new EventDeduplicator(TimeSpan.FromMilliseconds(_loggerOptions.DeduplicationWindowsMs));
        _metadataCollector = new FileMetaDataCollector(_loggerOptions);
        _serializer = new JsonLinesWriter();

        _channel = Channel.CreateBounded<FileEvent>(
            new BoundedChannelOptions(10_000)
            {
                FullMode = BoundedChannelFullMode.Wait,
                SingleWriter = false,
                SingleReader = true,
            }
        );
    }

    protected override async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        if (!Directory.Exists(_loggerOptions.WatchableDirectoryPath))
        {
            _logger.LogError($"The folder is not existst, path={_loggerOptions.WatchableDirectoryPath}");

            return;
        }

        _watcher = new FileSystemWatcher(_loggerOptions.WatchableDirectoryPath)
        {
            IncludeSubdirectories = _loggerOptions.IncludeSubdirectories,
            NotifyFilter = NotifyFilters.FileName
                         | NotifyFilters.DirectoryName
                         | NotifyFilters.LastWrite
                         | NotifyFilters.Size
                         | NotifyFilters.CreationTime,
            InternalBufferSize = 64 * 1024,
        };

        _watcher.Created += (_, e) => OnRawEvent(FileEventType.Created, e.FullPath, null);
        _watcher.Changed += (_, e) => OnRawEvent(FileEventType.Changed, e.FullPath, null);
        _watcher.Deleted += (_, e) => OnRawEvent(FileEventType.Deleted, e.FullPath, null);
        _watcher.Renamed += (_, e) => OnRawEvent(FileEventType.Renamed, e.FullPath, e.OldFullPath);
        _watcher.Error += (_, e) => _logger.LogError(e.GetException(), "Ошибка FileSystemWatcher");

        _watcher.EnableRaisingEvents = true;
        _logger.LogInformation($"Start watchin on {_loggerOptions.WatchableDirectoryPath}");


        // TODO: make consumer


    }


    private void OnRawEvent(FileEventType eventType, string path, string? oldPath)
    {

        bool isDir = Directory.Exists(path);

        try
        {
            var evt = new FileEvent()
            {
                EventType = eventType,
                FullPathToObject = path,
                OldFullPathToObject = oldPath,
                IsDirectory = isDir,
            };

            if (!_dedup.ShouldProcess(evt))
            {
                _logger.LogInformation($"Event is duplicated. Event={evt}, path={path}");
            }

            if (!_channel.Writer.TryWrite(evt))
            {
                _logger.LogWarning($"Channel is fulfilled, event is canceled: {path}");
            }

        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error during event processing {path}");
        }

    }

    private async Task ConsumeAsync(CancellationToken ct)
    {
        using var rotator = new LogRotator(_loggerOptions);

        await foreach (var evt in _channel.Reader.ReadAllAsync(ct))
        {
            try
            {
                long? size = null;
                string? sha = null;
                int? pid = null;
                string? procName = null;

                if (evt.EventType != FileEventType.Deleted)
                {
                    size = _metadataCollector.GetResourceObjectSize(evt.FullPathToObject, evt.IsDirectory);
                    sha = _metadataCollector.ComputeSHA256(evt.FullPathToObject, evt.IsDirectory);
                    (pid, procName) = _metadataCollector.TryGetLockingProcessInfo(evt.FullPathToObject);
                }

                var enriched = new FileEvent()
                {
                    Timestamp = evt.Timestamp,
                    EventType = evt.EventType,
                    FullPathToObject = evt.FullPathToObject,
                    OldFullPathToObject = evt.OldFullPathToObject,
                    IsDirectory = evt.IsDirectory,
                    SizeOfObject = size,
                    Sha256 = sha,
                    ProcessId = pid,
                    ProcessName = procName,
                };

                string line = _serializer.Serialize(enriched);
                rotator.WriteLogLine(line);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during write log into file");
            }
        }
    }

}