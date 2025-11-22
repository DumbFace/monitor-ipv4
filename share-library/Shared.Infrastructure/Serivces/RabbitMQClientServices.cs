using System.Text;
using Microsoft.Extensions.Options;
using monitor_ip_4_tool.Constant;
using monitor_ip_4_tool.Interfaces;
using monitor_ip_4_tool.Models;
using Newtonsoft.Json;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace monitor_ip_4_tool.Serivces
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
            var factory = new ConnectionFactory
            {
                HostName = "localhost", Port = 5672, UserName = "admin", Password = "admin",
            };
            var connection = await factory.CreateConnectionAsync();
            var channel = await connection.CreateChannelAsync();
            await channel.ExchangeDeclareAsync(exchange: ExchangeKeys.IP_CHANGED, type: ExchangeType.Fanout);

            var queueDeclareResult = await channel.QueueDeclareAsync(
                queue: _rabbitConfig.Queues.First(queue => queue == QueueKeys.FIREBASEUPDATE), durable: true,
                exclusive: false, autoDelete: false, arguments: null);
            string queueName = queueDeclareResult.QueueName;
            await channel.QueueBindAsync(queue: queueName, exchange: ExchangeKeys.IP_CHANGED,
                routingKey: String.Empty);
            var consumer = new AsyncEventingBasicConsumer(channel);
            consumer.ReceivedAsync += async (model, ea) =>
            {
                try
                {
                    var body = ea.Body.ToArray();
                    var ipv4 = Encoding.UTF8.GetString(body);
                    if (String.IsNullOrEmpty(ipv4)) throw new Exception("Ipv4 is null or empty ");
                    //TODO update firebase
                    _logger.Info($"Updated firebase unsuccessfully");
                    await Task.Delay(1000);
                    // throw new Exception();
                    // await _firebase.SaveIP(ipv4);
                    // _logger.Info($"Updated firebase successfully");
                    await channel.BasicAckAsync(deliveryTag: ea.DeliveryTag, multiple: false);
                }
                catch (Exception e)
                {
                    _logger.Error($"Message: {e.Message}");
                    await channel.BasicNackAsync(ea.DeliveryTag, false, true);
                }
            };

            await channel.BasicConsumeAsync(queueName, autoAck: false, consumer: consumer);
        }

        public async Task PublishMessageAsync<T>(T data, CancellationToken token = default)
        {
            var factory = new ConnectionFactory()
            {
                HostName = "localhost", Port = 5672, UserName = "admin", Password = "admin"
            };
            var connect = await factory.CreateConnectionAsync();
            var channel = await connect.CreateChannelAsync();
            var jsonData = JsonConvert.SerializeObject(data);
            var body = Encoding.UTF8.GetBytes(jsonData);
            await channel.ExchangeDeclareAsync(exchange: ExchangeKeys.IP_CHANGED, type: ExchangeType.Fanout);
            var properties = new BasicProperties { Persistent = true };
            channel.BasicReturnAsync += (sender, ea) =>
            {
                _logger.Error($"Message is returned \n ReplyCode: {ea.ReplyCode} \n ReplyText: {ea.ReplyText}");
                return Task.CompletedTask;
            };

            await channel.BasicPublishAsync(exchange: ExchangeKeys.IP_CHANGED, string.Empty, mandatory: true,
                basicProperties: properties, body: body);
        }
    }
}