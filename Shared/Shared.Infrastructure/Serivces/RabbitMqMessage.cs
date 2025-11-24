
using RabbitMQ.Client;
using Shared.Shared.Common.Interfaces;

namespace Shared.Shared.Infrastructure.Serivces
{
    public class RabbitMqMessage : IMessageBusClient
    {
        readonly private ILog _logger;
        readonly private IConnection _connection;

        public RabbitMqMessage(IConnection connection, ILog logger)
        {
            _connection = connection;
            _logger = logger;
        }

        public IPublisher CreatePublisher()
        {
            return new RabbitMqPublisher(_connection, _logger);
        }

        public ISubscriber CreateSubscriber()
        {

            return new RabbitMqSubscriber(_connection, _logger);

        }

    }
}