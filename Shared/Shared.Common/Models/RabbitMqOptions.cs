namespace Shared.Shared.Common.Models;

public class RabbitMqOptions : IMessagingOptions
{
    private string _defaultQueueName = String.Empty;
    public RabbitMqOptions(string defaultQueueName)
    {
        _defaultQueueName = defaultQueueName;
    }
    public string Queue => $"{_defaultQueueName}.queue";

    public string Exchange => $"{_defaultQueueName}.exchange";


    public string QueueRetry => $"{_defaultQueueName}.queue-retry";

    public string ExchangeRetry => $"{_defaultQueueName}.exchange-retry";


    public string RoutingKey => $"{_defaultQueueName}";

    public string DefaultQueueName => _defaultQueueName;

}
