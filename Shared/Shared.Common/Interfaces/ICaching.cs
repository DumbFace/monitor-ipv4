namespace Shared.Shared.Common.Interfaces;

public interface ICaching
{
    Task<T> GetAsync<T>(string key);

    Task SetAsync<T>(string key, T value, TimeSpan? expiration);
}
