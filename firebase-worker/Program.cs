using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

using RabbitMQ.Client;

using Serilog;

using Shared.Shared.Common.Constant;
using Shared.Shared.Common.Interfaces;
using Shared.Shared.Common.Models;
using Shared.Shared.Common.Utils;
using Shared.Shared.Infrastructure.Database;
using Shared.Shared.Infrastructure.Serivces;

namespace firebase_worker;

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

class Program
{
    private static async Task Main(string[] args)
    {
        using IHost host = Host.CreateDefaultBuilder(args).UseSerilog().UseWindowsService()
            .ConfigureAppConfiguration((context, config) =>
            {
                var env = context.HostingEnvironment.EnvironmentName;
                Console.WriteLine($"ENV: {env}");
                var path = env == EnvironmentEnum.DEV ? "appsettings.Development.json" : "appsettings.json";
                config.AddJsonFile(path, optional: false, reloadOnChange: true);
                var sharedPath = Path.Combine(AppContext.BaseDirectory, "sharedsettings.json");
                config.AddJsonFile(sharedPath, optional: false, reloadOnChange: true);

            }).ConfigureServices((context, services) =>
            {
                services.AddSingleton<ILog, LogServices>();
                services.AddOptions<RabbitMqConfig>().Bind(context.Configuration.GetSection(ConfigEnum.RABBITMQ));


                services.AddSingleton(context.Configuration);
                services.AddSingleton<ICustomHttpFactory, CustomHttpClientFactory>();

                services.AddSingleton<IMessageBusConnection<IConnection>, RabbitMQConnection>();

                services.AddSingleton<IMessageBusClient, RabbitMqMessage>();
                services.AddSharedLibrary(context.Configuration);
                services.AddSingleton<IDataCRUD, Firebase>();
                services.AddHostedService<FirebaseWorkerBackGroundService>();
            }).Build();

        await host.RunAsync();
    }
}
