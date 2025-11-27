using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using Polly;

using RabbitMQ.Client;

using Serilog;

using Shared.Shared.Common.Constant;
using Shared.Shared.Common.Interfaces;
using Shared.Shared.Common.Models;
using Shared.Shared.Common.Utils;
using Shared.Shared.Infrastructure.Caching;
using Shared.Shared.Infrastructure.Database;
using Shared.Shared.Infrastructure.Serivces;
namespace monitor_ip_4_tool;

public class MyBackGroundService : BackgroundService
{
    private readonly ICaching _memoryCache;
    private readonly IDatabase _database;
    private readonly ILog _logger;
    private readonly IInternetProtocol _ipv4Services;
    private readonly ISendMail _smtpService;
    private readonly IRetryHandler _retryHandler;
    private readonly IOpenVPN _openVPN;
    readonly IOptionsMonitor<SystemConfig> _systemConfigMonitor;
    private readonly ResiliencePipeline _pipeline;
    private readonly IMessageBusClient _messageBusClient;

    private readonly SystemConfig _systemConfig;
    public MyBackGroundService(
        IOpenVPN openVPN, ICaching memoryCache, IDatabase database, ILog logger,
        IInternetProtocol ipv4Services, ISendMail smtpService, IRetryHandler retryHandler,
        IPollyFactory pollyFactory, IOptionsMonitor<SystemConfig> systemConfigMonitor,
        IMessageBusClient messageBusClient)
    {
        _messageBusClient = messageBusClient;
        _openVPN = openVPN;
        _systemConfigMonitor = systemConfigMonitor;
        _pipeline = pollyFactory.GetIPServicesPipeLine();
        _retryHandler = retryHandler;
        _memoryCache = memoryCache;
        _database = database;
        _logger = logger;
        _ipv4Services = ipv4Services;
        _smtpService = smtpService;
        _systemConfig = systemConfigMonitor.CurrentValue;
        _systemConfigMonitor.OnChange((config) => { _logger.Info($"System change config {config.ToStringJson()}"); });
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            await Task.Delay(_systemConfig.ScanIPv4FromSecond, stoppingToken);
            try
            {
                string ipFromService = await _pipeline.ExecuteAsync(async (token) =>
                {
                    string ipv4 = String.Empty;
                    foreach (var url in _systemConfig.Ipv4Urls)
                    {
                        try
                        {
                            _logger.Info($"Call ip from url: {url}");
                            ipv4 = (await _ipv4Services.GetIP4Async(url, token)).CheckingIpv4(_logger);
                            if (!String.IsNullOrEmpty(ipv4)) return ipv4;
                        }

                        catch (Exception ex)
                        {
                            _logger.Error($"Error IPv4 service: {ex.Message}");
                        }
                    }
                    return ipv4;
                });

                if (String.IsNullOrEmpty(ipFromService))
                {
                    _logger.Info($"It is null or empty ip services: {ipFromService}");
                    continue;
                }

                _logger.Info($"{ipFromService}");

                var ipFromCaching = _memoryCache.Get<string>(Cachekeys.LAST_IP);
                var lastIp = ipFromCaching;
                if (String.IsNullOrEmpty(ipFromCaching))
                {
                    await _database.ConnectDb();
                    var ipFromDb = await _database.GetLastIP();
                    if (String.IsNullOrEmpty(ipFromDb))
                    {
                        await _database.InitDb();
                        lastIp = IP.LOCALIP;
                    }

                    lastIp = ipFromDb;
                    _memoryCache.Set(Cachekeys.LAST_IP, ipFromDb, null);
                    _logger.Info($"Ip from db:  {ipFromDb}");
                }

                _logger.Info($"Ip from caching:  {ipFromCaching}");
                _logger.Info($"Ip from service:  {ipFromService}");
                _logger.Info($"Ip from lastIp:  {lastIp}");

                if (lastIp == ipFromService)
                    continue;
                var producer = _messageBusClient.CreatePublisher();
                var rabbitOption = new RabbitMqOptions(RabbitMqMessageKeys.IP_CHANGED);

                await producer.PublishAsync(ipFromService, rabbitOption, stoppingToken);


                await _openVPN.UpdateClient(ipFromService);
                await _openVPN.RestartService(OperatingSystem.IsLinux() ?
                    _systemConfigMonitor.CurrentValue.LinuxOperating.OpenVPNService :
                    _systemConfigMonitor.CurrentValue.WindowOperating.OpenVPNService);
                await _retryHandler.ExecuteAsync((token) =>
                    _smtpService.SendMail(token, subject: "IP has changed", body: ipFromService));


                await _database.ConnectDb();
                await _database.SaveIP(ipFromService);
                await _database.CloseDb();

                _memoryCache.Set(Cachekeys.LAST_IP, ipFromService, null);

                _logger.Info("Update client openvpn");
                _logger.Info("Restart Service successfully");
            }
            catch (Exception ex)
            {
                _logger.Error($"Error: {ex.Message}");
            }
        }
    }

    public class Program
    {
        private static async Task Main(string[] args)
        {
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

                    services.AddHostedService<MyBackGroundService>();
                })

                .Build();
            await host.RunAsync();
        }
    }
}
