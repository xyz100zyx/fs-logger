using System.Collections.Concurrent;
using file_logger.Models;

namespace file_logger.Core;

using EventTypeAndFilePathKey = string;

public sealed class EventDeduplicator
{
    private readonly TimeSpan _window;
    private readonly ConcurrentDictionary<EventTypeAndFilePathKey, DateTimeOffset> _eventKeyToLastEventTimestampJoin = new();

    private DateTimeOffset _lastDictionaryCleanup = DateTimeOffset.UtcNow;

    public EventDeduplicator(TimeSpan window) => _window = window;

    /// <returns>
    /// true — событие нужно обработать, false — дубликат события / спам событий от какого-либо процесса
    /// </returns>
    public bool ShouldProcess(FileEvent e)
    {
        string EventKey = e.BuildKey();

        var eventTimestampNew = e.Timestamp;

        bool isDuplicate = false;

        this._eventKeyToLastEventTimestampJoin.AddOrUpdate(EventKey, _ => eventTimestampNew, (_, prevTimestamp) =>
            {


                if(eventTimestampNew - prevTimestamp < _window)
                {
                    isDuplicate = true;
                    return prevTimestamp;
                }

                return eventTimestampNew;
            }
        );

        CleanupIfNeeded(eventTimestampNew);
        return !isDuplicate;
    }

    private void CleanupIfNeeded(DateTimeOffset now)
    {
        var timeDiff = now - _lastDictionaryCleanup;

        /// если прошло меньше одной минуты, тогда просто скипаем очистку словаря
        if (timeDiff < TimeSpan.FromMinutes(1)) return;

        _lastDictionaryCleanup = now;

        var threshold = now - _window - TimeSpan.FromSeconds(5);

        foreach (var kvp in this._eventKeyToLastEventTimestampJoin)
        {
            if (kvp.Value < threshold)
                this._eventKeyToLastEventTimestampJoin.TryRemove(kvp.Key, out _);
        }
    }
    
    
}

