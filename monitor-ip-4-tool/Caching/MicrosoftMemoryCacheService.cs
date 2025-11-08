using Microsoft.Extensions.Caching.Memory;
using monitor_ip_4_tool.Interfaces;

namespace monitor_ip_4_tool.Caching;

public class MicrosoftMemoryCacheService : ICaching
{
    private readonly IMemoryCache _caching;

    public MicrosoftMemoryCacheService()
    {
        var options = new MemoryCacheOptions();
        _caching = new MemoryCache(options);
    }

    public T Get<T>(string key)
    {
        if (_caching.TryGetValue(key, out T value)) return value;

        return default;
    }

    public void Set<T>(string key, T value, TimeSpan? expiration = null)
    {
        var options = new MemoryCacheEntryOptions();
        if (expiration.HasValue) options.AbsoluteExpirationRelativeToNow = TimeSpan.FromDays(1);

        _caching.Set(key, value, options);
    }
}