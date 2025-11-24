namespace Shared.Shared.Common.Interfaces;

public interface ICaching
{
    T Get<T>(string key);

    void Set<T>(string key, T value, TimeSpan? expiration);
}