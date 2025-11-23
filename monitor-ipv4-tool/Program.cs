using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Polly;
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
    private readonly IEnumerable<IInternetProtocol> _ipv4Services;
    private readonly ISendMail _smtpService;
    private readonly IRetryHandler _retryHandler;
    private readonly IOpenVPN _openVPN;
    readonly IOptionsMonitor<SystemConfig> _systemConfigMonitor;
    private readonly ResiliencePipeline _pipeline;
    private readonly IMessageBroker _messageBroker;

    public MyBackGroundService(IOpenVPN openVPN, ICaching memoryCache, IDatabase database, ILog logger,
        IEnumerable<IInternetProtocol> ipv4Services, ISendMail smtpService, IRetryHandler retryHandler,
        IPollyFactory pollyFactory, IOptionsMonitor<SystemConfig> systemConfigMonitor, IMessageBroker messageBroker)
    {
        _messageBroker = messageBroker;
        _openVPN = openVPN;
        _systemConfigMonitor = systemConfigMonitor;
        _pipeline = pollyFactory.GetIPServicesPipeLine();
        _retryHandler = retryHandler;
        _memoryCache = memoryCache;
        _database = database;
        _logger = logger;
        _ipv4Services = ipv4Services;
        _smtpService = smtpService;

        _systemConfigMonitor.OnChange((config) => { _logger.Info($"System change config {config.ToStringJson()}"); });
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            await Task.Delay(_systemConfigMonitor.CurrentValue.ScanIPv4FromSecond, stoppingToken);

            try
            {
                // string ipFromService = await _pipeline.ExecuteAsync<string>(async (token) =>
                // {
                //     string ipv4 = String.Empty;
                //     foreach (var service in _ipv4Services)
                //     {
                //         try
                //         {
                //             ipv4 = await service.GetIP4Async(token);
                //             if (!String.IsNullOrEmpty(ipv4)) return ipv4;
                //         }
                //
                //         catch (Exception ex)
                //         {
                //             _logger.Error($"Error IPv4 service: {ex.Message}");
                //             Thread.Sleep(ThreadSleep.MONITOR_IP * 1000);
                //         }
                //     }
                //     return ipv4;
                // });

                string ipFromService = "192.168.1.1";

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
                //Durable data state
                await _messageBroker.PublishMessageAsync(ipFromService);

                await _database.ConnectDb();

                await _retryHandler.ExecuteAsync((token) =>
                    _smtpService.SendMail(token, subject: "IP has changed", body: ipFromService));
                _memoryCache.Set(Cachekeys.LAST_IP, ipFromService, null);
                await _database.SaveIP(ipFromService);
                await _database.CloseDb();

                _logger.Info("Update client openvpn");
                _logger.Info("Restart Service successfully");

                // await _openVPN.UpdateClient(ipFromService);
                // await _openVPN.RestartService(OperatingSystem.IsLinux() ?
                //     _systemConfigMonitor.CurrentValue.LinuxOperating.OpenVPNService :
                //     _systemConfigMonitor.CurrentValue.WindowOperating.OpenVPNService);
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
            using IHost host = Host.CreateDefaultBuilder(args).UseSerilog().UseWindowsService()
                .ConfigureAppConfiguration((context, config) =>
                {
                    var env = context.HostingEnvironment.EnvironmentName;
                    Console.WriteLine($"ENV: {env}");
                    var path = env == EnvironmentEnum.DEV ? "appsettings.Development.json" : "appsettings.json";
                    config.AddJsonFile(path, optional: false, reloadOnChange: true);


                    var sharedPath = Path.Combine(Directory.GetParent(Directory.GetCurrentDirectory()).FullName,
                                            "share-library",
                                            "sharedsettings.json"
                                        );
                    config.AddJsonFile(sharedPath, optional: false, reloadOnChange: true);
                    Console.WriteLine($"Shared Path: {sharedPath}");

                }).ConfigureServices((context, services) =>
                {
                    services.AddSingleton<ILog, LogServices>();

                    services.AddSingleton<IPollyFactory, PollyFactory>();
                    services.AddSingleton<IRetryHandler, RetryServices>();

                    services.AddSingleton(context.Configuration);
                    services.AddSingleton<ISendMail, SMTPService>();

                    //Alternative redis caching 
                    services.AddSingleton<ICaching, RedisCacheService>();
                    //    services.AddSingleton<ICaching, MicrosoftMemoryCacheService>();
                    services.AddSingleton<IInternetProtocol, IfConfigServices>();
                    services.AddSingleton<IInternetProtocol, IpifyService>();
                    services.AddSingleton<IDatabase, SqlLite>();
                    services.AddSingleton<ICustomHttpFactory, CustomHttpClientFactory>();

                    services.AddOptions<SystemConfig>().Bind(context.Configuration.GetSection(ConfigEnum.SYSTEM))
                        .ValidateDataAnnotations().ValidateOnStart();
                    services.AddOptions<RedisConfig>().Bind(context.Configuration.GetSection(ConfigEnum.REDIS));
                    services.AddOptions<SMTPConfig>().Bind(context.Configuration.GetSection(ConfigEnum.SMTP));
                    services.AddSharedLibrary(context.Configuration);
                    // services.AddOptions<RabbitMqConfig>().Bind(context.Configuration.GetSection(ConfigEnum.RABBITMQ));

                    services.AddSingleton<LinuxOpenVPNService>();
                    services.AddSingleton<WindowOpenVPNService>();
                    services.AddSingleton<IDataCRUD, Firebase>();
                    services.AddSingleton<IMessageBroker, RabbitMqClientServices>();


                    services.AddSingleton<IOpenVPN>(sp =>
                        OperatingSystem.IsLinux()
                            ? sp.GetRequiredService<LinuxOpenVPNService>()
                            : sp.GetRequiredService<WindowOpenVPNService>());

                    services.AddHostedService<MyBackGroundService>();
                }).Build();
            await host.RunAsync();
        }
    }
}