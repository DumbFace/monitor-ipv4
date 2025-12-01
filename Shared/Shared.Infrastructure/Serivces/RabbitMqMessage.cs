
using Shared.Shared.Common.Interfaces;

namespace Shared.Shared.Infrastructure.Serivces
{
    public class RabbitMqMessage : IMessageBusClient
    {
        private readonly Func<IPublisher> _publisherFactory;
        private readonly Func<ISubscriber> _subscriberFactory;


        public RabbitMqMessage(
            Func<IPublisher> publisherFactory,
            Func<ISubscriber> subscriberFactory)
        {
            _publisherFactory = publisherFactory;
            _subscriberFactory = subscriberFactory;
        }
        public IPublisher CreatePublisher()
               => _publisherFactory();

        public ISubscriber CreateSubscriber()
            => _subscriberFactory();
    }
}
