using System.Text;

using Newtonsoft.Json;

using RabbitMQ.Client;

using Shared.Shared.Common.Interfaces;
using Shared.Shared.Common.Models;

namespace Shared.Shared.Infrastructure.Serivces
{
    public class RabbitMqPublisher : IPublisher
    {
        readonly private IMessageBusConnection<IConnection> _rabbitConnection;
        private readonly IRetryHandler _retryHandler;

        readonly private ILog _logger;
        public RabbitMqPublisher(
            IMessageBusConnection<IConnection> rabbitConnection,
            ILog logger,
            IRetryHandler retryHandler
            )
        {
            _retryHandler = retryHandler;
            _logger = logger;
            _rabbitConnection = rabbitConnection;
        }

        public async Task PublishAsync<T, TConfig>(T data, TConfig option, CancellationToken token = default) where TConfig : IMessagingOptions
        {
            var rabbitOption = option as RabbitMqOptions
                                 ?? throw new InvalidOperationException("Config must be RabbitMqOptions.");
            var channel = await _retryHandler.ExecuteAsync(async (token) =>
            {
                var connection = await _rabbitConnection.GetConnectionAsync();
                return await connection.CreateChannelAsync(cancellationToken: token);
            });
            var jsonData = JsonConvert.SerializeObject(data);
            var body = Encoding.UTF8.GetBytes(jsonData);

            await channel.ExchangeDeclareAsync(exchange: rabbitOption.Exchange, type: ExchangeType.Direct, durable: true);
            var properties = new BasicProperties { Persistent = true };
            channel.BasicReturnAsync += (sender, ea) =>
            {
                _logger.Error($"Message is returned \n ReplyCode: {ea.ReplyCode} \n ReplyText: {ea.ReplyText}");
                return Task.CompletedTask;
            };

            await channel.BasicPublishAsync(exchange: rabbitOption.Exchange, routingKey: rabbitOption.RoutingKey, mandatory: true,
                basicProperties: properties, body: body);

            _logger.Info($"Send message to queue:  {body}");

        }
    }
}
