using Microsoft.Extensions.Caching.Memory;

namespace monitor_ip_4_tool.Interfaces;

public interface ICaching
{
    T Get<T>(string key);

    void Set<T>(string key, T value, TimeSpan? expiration);
}