using monitor_ip_4_tool.Interfaces;
using Newtonsoft.Json;
using StackExchange.Redis;
using IDatabase = monitor_ip_4_tool.Interfaces.IDatabase;

namespace monitor_ip_4_tool.Caching;

public class RedisCacheService : ICaching, IDisposable
{
    private StackExchange.Redis.IDatabase _db;
    private ConnectionMultiplexer _connection;

    public RedisCacheService()
    {
        _connection = ConnectionMultiplexer.Connect("localhost:6379");
        _db = _connection.GetDatabase();
        var pong = _db.Ping();
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