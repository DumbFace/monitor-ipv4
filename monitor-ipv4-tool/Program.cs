
using System.CommandLine;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

using RabbitMQ.Client;

using Serilog;

using Shared.Shared.Common.Constant;
using Shared.Shared.Common.Interfaces;
using Shared.Shared.Common.Models;
using Shared.Shared.Infrastructure.Caching;
using Shared.Shared.Infrastructure.Database;
using Shared.Shared.Infrastructure.Serivces;

namespace monitor_ip_4_tool;

public class Program
{
    private static async Task<int> Main(string[] args)
    {

        var rabbitOption = new Option<bool>("--rabbitmq");
        rabbitOption.Description = "Enable RabbitMQ";
        rabbitOption.DefaultValueFactory = (_) => true;

        var sendMailOption = new Option<bool>("--sendmail");
        sendMailOption.Description = "Enable SendMail";
        sendMailOption.DefaultValueFactory = (_) => true;

        var root = new RootCommand("Monitor tool")
            {
                rabbitOption,
                sendMailOption
            };
        root.SetAction(async parseResult =>
       {
           bool rabbitmq = parseResult.GetValue(rabbitOption);
           bool sendmail = parseResult.GetValue(sendMailOption);

           using IHost host = Host.CreateDefaultBuilder(args)
               .UseWindowsService()
               .ConfigureAppConfiguration((context, config) =>
               {
                   var env = context.HostingEnvironment.EnvironmentName;
                   var path = env == Environments.Development ? "appsettings.Development.json" : "appsettings.Production.json";
                   config.AddJsonFile(path, optional: false, reloadOnChange: true);
               })
               .UseSerilog((context, service, config) =>
               {
                   config.ReadFrom.Configuration(context.Configuration);
               })
               .ConfigureServices((context, services) =>
               {
                   services.AddLogging((logger) =>
                   {
                       logger.ClearProviders();
                       logger.AddSerilog();
                   });
                   services.AddSingleton<ILog, LogServices>();
                   services.AddSingleton(new CliOptions
                   {
                       RabbitMq = rabbitmq,
                       SendMail = sendmail,
                   });
                   services.AddSingleton<IPollyFactory, PollyFactory>();
                   services.AddSingleton<IRetryHandler, RetryServices>();
                   services.AddOptions<SystemConfig>().Bind(context.Configuration.GetSection(ConfigEnum.SYSTEM))
                       .ValidateDataAnnotations().ValidateOnStart();
                   services.AddOptions<RabbitMqConfig>().Bind(context.Configuration.GetSection(ConfigEnum.RABBITMQ));
                   services.AddOptions<Ipv4Config>().Bind(context.Configuration.GetSection(ConfigEnum.Ipv4Config));

                   services.AddOptions<RedisConfig>().Bind(context.Configuration.GetSection(ConfigEnum.REDIS));
                   services.AddOptions<SMTPConfig>().Bind(context.Configuration.GetSection(ConfigEnum.SMTP));
                   services.AddSingleton(context.Configuration);
                   services.AddSingleton<ISendMail, SMTPService>();

                   services.AddSingleton<ICaching, RedisCacheService>();
                   services.AddSingleton<IInternetProtocol, Ipv4Services>();
                   services.AddSingleton<IDatabase, SqlLite>();
                   services.AddSingleton<ICustomHttpFactory, CustomHttpClientFactory>();

                   services.AddSingleton<LinuxOpenVPNService>();
                   services.AddSingleton<WindowOpenVPNService>();
                   services.AddSingleton<IDataCRUD, Firebase>();
                   services.AddSingleton<IMessageBusConnection<IConnection>, RabbitMQConnection>();

                   services.AddSingleton<IMessageBusClient, RabbitMqMessage>();
                   services.AddSingleton<IPublisher, RabbitMqPublisher>();
                   services.AddSingleton<ISubscriber, RabbitMqSubscriber>();

                   services.AddSingleton<IOpenVPN>(sp =>
                       OperatingSystem.IsLinux()
                           ? sp.GetRequiredService<LinuxOpenVPNService>()
                           : sp.GetRequiredService<WindowOpenVPNService>());

                   services.AddHostedService<MonitorIpv4ServerService>();
               })
               .Build();
           await host.RunAsync();

       });

        return await root.Parse(args).InvokeAsync();
    }
}

