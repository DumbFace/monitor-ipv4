using System.Runtime.InteropServices;
using System.Text;
using Castle.DynamicProxy;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using Serilog;
using Shared.Shared.Common.Constant;
using Shared.Shared.Common.Interfaces;
using Shared.Shared.Common.Models;
using Shared.Shared.Common.Utils;
using Shared.Shared.Infrastructure.Database;
using Shared.Shared.Infrastructure.Serivces;
using SQLitePCL;

namespace firebase_worker;

public class FirebaseWorkerBackGroundService : BackgroundService
{
    private readonly IMessageBroker _messageBroker;

    private readonly IMessageBusClient _messageBusClient;
    public FirebaseWorkerBackGroundService(IMessageBroker messageBroker, IMessageBusClient messageBusClient)
    {
        _messageBusClient = messageBusClient;
        _messageBroker = messageBroker;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var consumer = _messageBusClient.CreateSubscriber();
        await consumer.SubscribeAsync(() => { }, stoppingToken);
        // await _messageBroker.RetryMessageAsync(stoppingToken);
        // await _messageBroker.SubscribeMessageAsync(token: stoppingToken);
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
                var sharedPath = Path.Combine(Directory.GetParent(Directory.GetCurrentDirectory()).FullName,
                                        "Shared",
                                        "sharedsettings.json"
                                    );
                config.AddJsonFile(sharedPath, optional: false, reloadOnChange: true);
                Console.WriteLine($"Shared Path: {sharedPath}");

            }).ConfigureServices((context, services) =>
            {
                services.AddSingleton<ILog, LogServices>();
                services.AddOptions<RabbitMqConfig>().Bind(context.Configuration.GetSection(ConfigEnum.RABBITMQ));


                services.AddSingleton(context.Configuration);
                services.AddSingleton<ICustomHttpFactory, CustomHttpClientFactory>();

                services.AddSingleton<IMessageBusClient, RabbitMqMessage>();
                services.AddSharedLibrary(context.Configuration);
                services.AddSingleton<IDataCRUD, Firebase>();
                services.AddSingleton<IMessageBroker, RabbitMqClientServices>();

                services.AddHostedService<FirebaseWorkerBackGroundService>();
            }).Build();

        await host.RunAsync();
    }
}