namespace Shared.Shared.Common.Interfaces
{
    public interface IMessageBusClient
    {
        IPublisher CreatePublisher();
        ISubscriber CreateSubscriber();
    }
}