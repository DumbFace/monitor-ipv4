using System.Text;
using Newtonsoft.Json;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using Shared.Shared.Common.Interfaces;
using Shared.Shared.Common.Models;

namespace Shared.Shared.Infrastructure.Serivces
{
    public class RabbitMqSubscriber : ISubscriber
    {
        readonly private IMessageBusConnection<IConnection> _rabbitConnection;
        readonly private ILog _logger;

        public RabbitMqSubscriber(
            IMessageBusConnection<IConnection> rabbitConnection,
            ILog logger
            )
        {
            _logger = logger;
            _rabbitConnection = rabbitConnection;
        }

        public async Task SubscribeAsync<T, TConfig>(Func<T, Task> handler, TConfig config, CancellationToken token = default) where TConfig : IMessagingOptions
        {
            var option = config as RabbitMqOptions
                             ?? throw new InvalidOperationException("Config must be RabbitMqOptions.");
            var connection = await _rabbitConnection.GetConnectionAsync();
            var channel = await connection.CreateChannelAsync();
            try
            {
                var queueRetryArg = new Dictionary<string, object>
                    {
                        { "x-dead-letter-exchange", option.Exchange },
                        { "x-message-ttl", 5000 },
                    };

                var queueArg = new Dictionary<string, object>
                    {
                    { "x-dead-letter-exchange", option.ExchangeRetry }
                    };
                await channel.BasicQosAsync(0, 1, false);

                await channel.QueueDeclareAsync(
                    queue: option.QueueRetry, durable: true,
                    exclusive: false, autoDelete: false, arguments: queueRetryArg);
                await channel.ExchangeDeclareAsync(exchange: option.ExchangeRetry, type: ExchangeType.Direct, durable: true);

                await channel.QueueBindAsync(queue: option.QueueRetry, exchange: option.ExchangeRetry, routingKey: option.RoutingKey);

                await channel.BasicQosAsync(0, 1, false);

                await channel.ExchangeDeclareAsync(exchange: option.Exchange, type: ExchangeType.Direct, durable: true);

                await channel.QueueDeclareAsync(
                   queue: option.Queue, durable: true,
                   exclusive: false, autoDelete: false, arguments: queueArg);
                await channel.QueueBindAsync(queue: option.Queue, exchange: option.Exchange,
                    routingKey: option.RoutingKey);
                var consumer = new AsyncEventingBasicConsumer(channel);

                consumer.ReceivedAsync += async (model, ea) =>
                {
                    try
                    {
                        var bodyBytes = ea.Body.ToArray();
                        var bodyString = Encoding.UTF8.GetString(bodyBytes);
                        var data = JsonConvert.DeserializeObject<T>(bodyString);
                        _logger.Info($"Received message from queue. {data}");
                        await handler(data);
                        await channel.BasicAckAsync(deliveryTag: ea.DeliveryTag, multiple: false);
                    }
                    catch (Exception e)
                    {
                        _logger.Info($"Updated firebase unsuccessfully");
                        _logger.Error($"Handle Message Unsuccessful, send back to exchange: {e.Message}");
                        await channel.BasicNackAsync(ea.DeliveryTag, false, false);
                    }
                };

                await channel.BasicConsumeAsync(option.Queue, autoAck: false, consumer: consumer);

            }
            catch (Exception e)
            {
                _logger.Error($"Subscribe message exception: {e.Message}");
            }
        }
    }
}