using System.Collections.Concurrent;
using System.Linq;

public class MessageCache
{
    private readonly ConcurrentDictionary<string, MessageContent> _cache = new();
    private readonly int _maxCacheSize;

    public MessageCache(int maxCacheSize = 1000)
    {
        _maxCacheSize = maxCacheSize;
    }

    public bool TryAdd(string messageId, MessageContent message)
    {
        if (_cache.Count >= _maxCacheSize)
        {
            // 移除最早的消息
            var oldestKey = _cache.Keys.FirstOrDefault();
            if (oldestKey != null)
            {
                _cache.TryRemove(oldestKey, out _);
            }
        }

        return _cache.TryAdd(messageId, message);
    }

    public bool Contains(string messageId)
    {
        return _cache.ContainsKey(messageId);
    }

    public void Clear()
    {
        _cache.Clear();
    }
} 