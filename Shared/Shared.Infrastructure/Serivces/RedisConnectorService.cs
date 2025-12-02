
using Microsoft.Extensions.Options;

using Shared.Shared.Common.Interfaces;
using Shared.Shared.Common.Models;

using StackExchange.Redis;

namespace Shared.Shared.Infrastructure.Serivces
{
    public class RedisConnectorService : ICachingConnector
    {
        private readonly ILog _logger;

        private readonly IRetryHandler _retryHandler;
        readonly IOptionsMonitor<RedisConfig> _redisConfigMonitor;

        public RedisConnectorService(
            IRetryHandler retryHandler,
            ILog logger,
            IOptionsMonitor<RedisConfig> redisConfigMonitor
        )
        {
            _retryHandler = retryHandler;
            _redisConfigMonitor = redisConfigMonitor;
            _logger = logger;
        }

        public async Task<ConnectionMultiplexer> GetConnectionMultiplexerAsync()
        {
            RedisConfig redisConfig = _redisConfigMonitor.CurrentValue;
            var redisOptions = new ConfigurationOptions
            {
                EndPoints = { $"{redisConfig.Server}:{redisConfig.Port}" },

                SyncTimeout = 5000,
                AsyncTimeout = 5000,

                ConnectTimeout = 5000,
                AbortOnConnectFail = false,
                ReconnectRetryPolicy = new ExponentialRetry(5000),
                KeepAlive = 180,
                ConnectRetry = 3
            };
            if (redisConfig is null) throw new Exception("Cannot read redis config or redis is null");

            var connector = await ConnectionMultiplexer.ConnectAsync(redisOptions);

            return connector;
        }
    }
}
