using System.Text;
using Newtonsoft.Json;
using RabbitMQ.Client;
using Shared.Shared.Common.Constant;
using Shared.Shared.Common.Interfaces;

namespace Shared.Shared.Infrastructure.Serivces
{
    public class RabbitMqPublisher : IPublisher
    {
        readonly private ILog _logger;
        readonly private IConnection _connection;
        public RabbitMqPublisher(
            IConnection connection,
            ILog logger
            )
        {
            _logger = logger;
            _connection = connection;
        }

        public async Task PublishAsync(string queue, object data, CancellationToken token = default)
        {
            var channel = await _connection.CreateChannelAsync();
            var exchange = $"{RabbitMqMessageKeys.IP_CHANGED}.exchange";
            var routing = RabbitMqMessageKeys.IP_CHANGED;
            var jsonData = JsonConvert.SerializeObject(data);
            var body = Encoding.UTF8.GetBytes(jsonData);

            await channel.ExchangeDeclareAsync(exchange: exchange, type: ExchangeType.Direct, durable: true);
            var properties = new BasicProperties { Persistent = true };
            channel.BasicReturnAsync += (sender, ea) =>
            {
                _logger.Error($"Message is returned \n ReplyCode: {ea.ReplyCode} \n ReplyText: {ea.ReplyText}");
                return Task.CompletedTask;
            };

            await channel.BasicPublishAsync(exchange: exchange, routingKey: routing, mandatory: true,
                basicProperties: properties, body: body);
        }
    }
}