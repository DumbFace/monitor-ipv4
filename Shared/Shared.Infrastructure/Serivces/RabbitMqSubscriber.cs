using System.ComponentModel;
using System.Text;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using Shared.Shared.Common.Constant;
using Shared.Shared.Common.Interfaces;

namespace Shared.Shared.Infrastructure.Serivces
{
    public class RabbitMqSubscriber : ISubscriber
    {
        readonly private IConnection _connection;
        readonly private ILog _logger;

        public RabbitMqSubscriber(
            IConnection connection,
            ILog logger
            )
        {
            _logger = logger;
            _connection = connection;
        }

        public async Task SubscribeAsync(Action handler, CancellationToken token = default)
        {
            var channel = await _connection.CreateChannelAsync();
            try
            {
                var exchange = $"{RabbitMqMessageKeys.IP_CHANGED}.exchange";
                var retryExchange = $"{RabbitMqMessageKeys.IP_CHANGED}.retry-exchange";
                var routing = $"{RabbitMqMessageKeys.IP_CHANGED}.routing";
                var queue = $"{RabbitMqMessageKeys.IP_CHANGED}.queue";
                var queueRetry = $"{RabbitMqMessageKeys.IP_CHANGED}.retry";



                var queueRetryArg = new Dictionary<string, object>
                    {
                        { "x-dead-letter-exchange", exchange },
                        { "x-message-ttl", 5000 },
                    };

                var queueArg = new Dictionary<string, object>
                    {
                    { "x-dead-letter-exchange", retryExchange }
                    };

                await channel.QueueDeclareAsync(
                    queue: queueRetry, durable: true,
                    exclusive: false, autoDelete: false, arguments: queueRetryArg);
                await channel.ExchangeDeclareAsync(exchange: retryExchange, type: ExchangeType.Direct, durable: true);

                await channel.QueueBindAsync(queue: queueRetry, exchange: retryExchange, routingKey: routing);

                await channel.BasicQosAsync(0, 1, false);

                await channel.ExchangeDeclareAsync(exchange: exchange, type: ExchangeType.Direct, durable: true);

                await channel.QueueDeclareAsync(
                   queue: queue, durable: true,
                   exclusive: false, autoDelete: false, arguments: queueArg);
                await channel.QueueBindAsync(queue: queue, exchange: exchange,
                    routingKey: routing);
                var consumer = new AsyncEventingBasicConsumer(channel);
                consumer.ReceivedAsync += async (model, ea) =>
                {
                    try
                    {
                        var body = ea.Body.ToArray();
                        var ipv4 = Encoding.UTF8.GetString(body);
                        if (String.IsNullOrEmpty(ipv4)) throw new Exception("Ipv4 is null or empty ");
                        //TODO update firebase
                        _logger.Info($"Processing update firebase");

                        await Task.Delay(5000);
                        throw new Exception();
                        // await _firebase.SaveIP(ipv4);
                        // _logger.Info($"Updated firebase successfully");
                        await channel.BasicAckAsync(deliveryTag: ea.DeliveryTag, multiple: false);
                    }
                    catch (Exception e)
                    {
                        _logger.Info($"Updated firebase unsuccessfully");
                        _logger.Error($"Handle Message Unsuccessful, send back to exchange: {e.Message}");
                        await channel.BasicNackAsync(ea.DeliveryTag, false, false);
                    }
                };

                await channel.BasicConsumeAsync(queue, autoAck: false, consumer: consumer);

            }
            catch (Exception e)
            {
                _logger.Error($"Subscribe message exception: {e.Message}");
            }
        }
    }
}