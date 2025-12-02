using StackExchange.Redis;

namespace Shared.Shared.Common.Interfaces;

public interface ICachingConnector
{
    Task<ConnectionMultiplexer> GetConnectionMultiplexerAsync();
}
