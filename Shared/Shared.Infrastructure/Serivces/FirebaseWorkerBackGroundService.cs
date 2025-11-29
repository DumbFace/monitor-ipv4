using Microsoft.Extensions.Hosting;

using Shared.Shared.Common.Constant;
using Shared.Shared.Common.Interfaces;
using Shared.Shared.Common.Models;

namespace Shared.Shared.Infrastructure.Serivces;

public class FirebaseWorkerBackGroundService : BackgroundService
{
    private readonly IDataCRUD _firebase;
    private readonly IMessageBusClient _messageBusClient;

    public FirebaseWorkerBackGroundService(

        IDataCRUD firebase,
        IMessageBusClient messageBusClient)
    {
        _firebase = firebase;
        _messageBusClient = messageBusClient;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var consumer = _messageBusClient.CreateSubscriber();
        var rabbitMqOptions = new RabbitMqOptions(RabbitMqMessageKeys.IP_CHANGED);
        await consumer.SubscribeAsync<string, RabbitMqOptions>(async (ipv4) =>
        {
            await _firebase.SaveIP(ipv4, stoppingToken);
        }, rabbitMqOptions, stoppingToken);
    }
}
