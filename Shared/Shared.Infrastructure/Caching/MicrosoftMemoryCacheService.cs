using Microsoft.Extensions.Caching.Memory;

using Shared.Shared.Common.Interfaces;

namespace Shared.Shared.Infrastructure.Caching;

public class MicrosoftMemoryCacheService : ICaching
{
    private readonly IMemoryCache _caching;
    private readonly ILog _logger;
    public MicrosoftMemoryCacheService(
        ILog logger
    )
    {
        _logger = logger;
        var options = new MemoryCacheOptions();
        _caching = new MemoryCache(options);
        _logger.Info("Init Memory Cache Service");
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
