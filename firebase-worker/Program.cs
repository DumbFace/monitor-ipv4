using System.Runtime.InteropServices;
using System.Text;
using Castle.DynamicProxy;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using monitor_ip_4_tool.Constant;
using monitor_ip_4_tool.Database;
using monitor_ip_4_tool.Interfaces;
using monitor_ip_4_tool.Models;
using monitor_ip_4_tool.Serivces;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using Serilog;
using SQLitePCL;

namespace firebase_worker;

public class FirebaseWorkerBackGroundService : BackgroundService
{
    private readonly IMessageBroker _messageBroker;

    public FirebaseWorkerBackGroundService(IMessageBroker messageBroker)
    {
        _messageBroker = messageBroker;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await _messageBroker.SubscribeMessageAsync(token: stoppingToken);
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
                Console.WriteLine($"File: {path} ");
            }).ConfigureServices((context, services) =>
            {
                services.AddSingleton<ILog, LogServices>();
                services.AddOptions<RabbitMqConfig>().Bind(context.Configuration.GetSection(ConfigEnum.RABBITMQ));


                services.AddSingleton(context.Configuration);
                services.AddSingleton<ICustomHttpFactory, CustomHttpClientFactory>();

                // services.AddOptions<FirebaseConfig>().Bind(context.Configuration.GetSection(ConfigEnum.FIREBASE));

                services.AddSingleton<IDataCRUD, Firebase>();
                services.AddSingleton<IMessageBroker, RabbitMqClientServices>();

                services.AddHostedService<FirebaseWorkerBackGroundService>();
            }).Build();

        await host.RunAsync();
    }
}