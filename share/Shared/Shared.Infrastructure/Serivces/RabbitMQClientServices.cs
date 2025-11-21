
using System.Text;
using monitor_ip_4_tool.Constant;
using monitor_ip_4_tool.Interfaces;
using Newtonsoft.Json;
using RabbitMQ.Client;

namespace monitor_ip_4_tool.Serivces
{
    public class RabbitMQClientServices : IMessageBroker
    {
        readonly private ILog _logger;

        public RabbitMQClientServices(ILog logger)
        {
            _logger = logger;
        }

        public Task SubscribeMessageAsync<T>(T data)
        {
            return Task.CompletedTask;
        }

        public async Task PublishMessageAsync<T>(T data)
        {
            var factory = new ConnectionFactory()
            {
                HostName = "localhost",
                Port= 5672,
                UserName = "admin",
                Password = "admin"
            };
            var connect = await factory.CreateConnectionAsync();
            var channel = await connect.CreateChannelAsync();
            var jsonData = JsonConvert.SerializeObject(data);
            var body = Encoding.UTF8.GetBytes(jsonData);
            await channel.ExchangeDeclareAsync(exchange: ExchangeKeys.IP_CHANGED, type: ExchangeType.Fanout);
            var properties = new BasicProperties
            {
                Persistent = true
            };
            channel.BasicReturnAsync += (sender, ea) =>
            {
                _logger.Error($"Message is returned \n ReplyCode: {ea.ReplyCode} \n ReplyText: {ea.ReplyText}");
                return Task.CompletedTask;
            };

            await channel.BasicPublishAsync(
                        exchange: ExchangeKeys.IP_CHANGED,
                        string.Empty,
                        mandatory: true,
                        basicProperties: properties,
                        body: body
                    );
        }
    }
}