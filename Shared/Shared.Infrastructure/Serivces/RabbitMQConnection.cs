

using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using Shared.Shared.Common.Interfaces;
using Shared.Shared.Common.Models;
namespace Shared.Shared.Infrastructure.Serivces
{
    public class RabbitMQConnection : IMessageBusConnection<IConnection>
    {
        readonly private ConnectionFactory _factory;
        public IConnection _connector { get; set; }
        readonly IOptionsMonitor<RabbitMqConfig> _rabbitConfigMonitor;


        public RabbitMQConnection(IOptionsMonitor<RabbitMqConfig> rabbitConfigMonitor)
        {
            _rabbitConfigMonitor = rabbitConfigMonitor;
            RabbitMqConfig _rabbitConfig = _rabbitConfigMonitor.CurrentValue;

            _factory = new ConnectionFactory
            {
                HostName = _rabbitConfig.Host,
                Port = _rabbitConfig.Port,
                UserName = _rabbitConfig.Username,
                Password = _rabbitConfig.Password,
            };
        }


        public async Task<IConnection> GetConnectionAsync()
        {
            if (_connector is { IsOpen: true })
            {
                return _connector;
            }

            _connector = await _factory.CreateConnectionAsync();
            return _connector;
        }
    }


}