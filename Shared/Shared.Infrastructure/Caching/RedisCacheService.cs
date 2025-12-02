using Microsoft.Extensions.Options;

using Newtonsoft.Json;

using Shared.Shared.Common.Interfaces;
using Shared.Shared.Common.Models;

using StackExchange.Redis;

namespace Shared.Shared.Infrastructure.Caching;

public class RedisCacheService : ICaching
{
    private ILog _logger;
    private readonly IRetryHandler _retryHandler;
    private readonly ICachingConnector _connector;


    public RedisCacheService(
        ILog logger,
        ICachingConnector connector,
        IRetryHandler retryHandler
    )
    {
        _retryHandler = retryHandler;
        _connector = connector;
        _logger = logger;
    }

    public async Task<T> GetAsync<T>(string key)
    {
        var redisConnection = await _connector.GetConnectionMultiplexerAsync();
        var db = redisConnection.GetDatabase();
        var value = db.StringGet(key);
        if (value.IsNullOrEmpty) return default;
        return JsonConvert.DeserializeObject<T>(value);
    }

    public async Task SetAsync<T>(string key, T value, TimeSpan? expiration = null)
    {
        var redisConnection = await _connector.GetConnectionMultiplexerAsync();
        var db = redisConnection.GetDatabase();
        TimeSpan timeSpan = expiration ?? TimeSpan.FromHours(1);
        var serilizeObject = JsonConvert.SerializeObject(value);
        db.StringSet(key, serilizeObject, timeSpan);
    }
}
