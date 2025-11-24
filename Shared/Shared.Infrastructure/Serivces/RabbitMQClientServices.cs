using System.Text;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using Shared.Shared.Common.Constant;
using Shared.Shared.Common.Interfaces;
using Shared.Shared.Common.Models;

namespace Shared.Shared.Infrastructure.Serivces
{
    public class RabbitMqClientServices : IMessageBroker
    {
        readonly private ILog _logger;
        readonly private IDataCRUD _firebase;
        readonly private IOptionsMonitor<RabbitMqConfig> _rabbitMqMonitor;
        readonly private RabbitMqConfig _rabbitConfig;

        public RabbitMqClientServices(ILog logger, IDataCRUD firebase, IOptionsMonitor<RabbitMqConfig> config)
        {
            _firebase = firebase;
            _logger = logger;
            _rabbitMqMonitor = config;
            _rabbitConfig = config.CurrentValue;

            _rabbitMqMonitor.OnChange((config) => _logger.Info($"RabbitMq config changed: {config.ToStringJson()}"));
            _logger.Info($"RabbitmqConfig: {config.CurrentValue.ToStringJson()}");
        }

        public async Task SubscribeMessageAsync(CancellationToken token = default)
        {
            try
            {
                var factory = new ConnectionFactory
                {
                    HostName = "localhost",
                    Port = 5672,
                    UserName = "admin",
                    Password = "admin",
                };
                var connection = await factory.CreateConnectionAsync();
                var channel = await connection.CreateChannelAsync();

                await channel.BasicQosAsync(0, 1, false);

                await channel.ExchangeDeclareAsync(exchange: ExchangeKeys.IP_CHANGED, type: ExchangeType.Direct, durable: true);


                //TODO Remove comment later
                // var queueNameConfig = _rabbitConfig.Queues.Select(queue => queue == QueueKeys.FIREBASEUPDATE ? queue : String.Empty)
                //     .FirstOrDefault();
                // _logger.Info($"Queue name from config: {queueNameConfig}");
                var queueDeclareResult = await channel.QueueDeclareAsync(
                    queue: "firebase-update-dev", durable: true,
                    exclusive: false, autoDelete: false, arguments: new Dictionary<string, object>
                                {
                                    { "x-dead-letter-exchange", "retry-exchange" },
                                    // { "x-message-ttl", 5000 },              // TTL 5s
                                    // { "x-dead-letter-routing-key", "retry" } // optional
                                });
                string queueName = queueDeclareResult.QueueName;
                await channel.QueueBindAsync(queue: queueName, exchange: ExchangeKeys.IP_CHANGED,
                    routingKey: "task");
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

                await channel.BasicConsumeAsync(queueName, autoAck: false, consumer: consumer);

            }
            catch (Exception e)
            {
                _logger.Error($"Subscribe message exception: {e.Message}");
            }

        }

        public async Task PublishMessageAsync<T>(T data, CancellationToken token = default)
        {
            var factory = new ConnectionFactory()
            {
                HostName = "localhost",
                Port = 5672,
                UserName = "admin",
                Password = "admin"
            };
            var connect = await factory.CreateConnectionAsync();
            var channel = await connect.CreateChannelAsync();
            var jsonData = JsonConvert.SerializeObject(data);
            var body = Encoding.UTF8.GetBytes(jsonData);
            await channel.ExchangeDeclareAsync(exchange: ExchangeKeys.IP_CHANGED, type: ExchangeType.Direct, durable: true);
            var properties = new BasicProperties { Persistent = true };
            channel.BasicReturnAsync += (sender, ea) =>
            {
                _logger.Error($"Message is returned \n ReplyCode: {ea.ReplyCode} \n ReplyText: {ea.ReplyText}");
                return Task.CompletedTask;
            };

            await channel.BasicPublishAsync(exchange: ExchangeKeys.IP_CHANGED, "task", mandatory: true,
                basicProperties: properties, body: body);
        }

        public async Task RetryMessageAsync(CancellationToken token = default)
        {

            var factory = new ConnectionFactory()
            {
                HostName = "localhost",
                Port = 5672,
                UserName = "admin",
                Password = "admin"
            };
            var connect = await factory.CreateConnectionAsync();
            var channel = await connect.CreateChannelAsync();

            var queueRetryArg = new Dictionary<string, object>
            {
                { "x-dead-letter-exchange", ExchangeKeys.IP_CHANGED },
                { "x-message-ttl", 5000 },
            };


            var queue = await channel.QueueDeclareAsync(
                queue: "firebase-update.retry", durable: true,
                exclusive: false, autoDelete: false, arguments: queueRetryArg);

            await channel.ExchangeDeclareAsync(exchange: ExchangeKeys.retry_exchange, type: ExchangeType.Direct, durable: true);


            await channel.QueueBindAsync(queue: queue.QueueName, exchange: ExchangeKeys.retry_exchange, routingKey: "task");
        }
    }
}