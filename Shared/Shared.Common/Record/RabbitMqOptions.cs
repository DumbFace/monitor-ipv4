namespace Shared.Shared.Common.Record
{
    public record RabbitMqOptions(string DefaultQueueName)
    {
        public string Queue => $"{DefaultQueueName}.queue";
        public string Exchange => $"{DefaultQueueName}.exchange";

        public string QueueRetry => $"{DefaultQueueName}.queue-retry";
        public string ExchangeRetry => $"{DefaultQueueName}.exchange-retry";

        public string RoutingKey => DefaultQueueName;
    }
}