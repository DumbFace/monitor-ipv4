using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Shared.Shared.Common.Interfaces;
using Shared.Shared.Common.Models;
using StackExchange.Redis;

namespace Shared.Shared.Infrastructure.Caching;

public class RedisCacheService : ICaching, IDisposable
{
    private StackExchange.Redis.IDatabase _db;
    private ConnectionMultiplexer _connection;
    private ILog _logger;

    readonly IOptionsMonitor<RedisConfig> _redisConfigMonitor;

    public RedisCacheService(
        ILog logger,
        IOptionsMonitor<RedisConfig> redisConfigMonitor
    )
    {
        _redisConfigMonitor = redisConfigMonitor;
        _logger = logger;
        RedisConfig redisConfig = _redisConfigMonitor.CurrentValue;
        if (redisConfig is null) throw new Exception("Cannot read redis config or redis is null");
        _connection = ConnectionMultiplexer.Connect($"{redisConfig.Server}:{redisConfig.Port}");
        _db = _connection.GetDatabase();
        _logger.Info($"Redis DB is ready {_db.Ping()}");
        _redisConfigMonitor.OnChange((config) =>
        {
            _logger.Info($"Redis change config {config.ToStringJson()}");
        });
    }

    public T Get<T>(string key)
    {
        var value = _db.StringGet(key);
        if (value.IsNullOrEmpty) return default;
        return JsonConvert.DeserializeObject<T>(value);
    }

    public void Set<T>(string key, T value, TimeSpan? expiration = null)
    {
        TimeSpan timeSpan = expiration ?? TimeSpan.FromHours(1);
        var serilizeObject = JsonConvert.SerializeObject(value);
        _db.StringSet(key, serilizeObject, timeSpan);
    }

    public void Dispose()
    {
        _connection.Dispose();
    }
}