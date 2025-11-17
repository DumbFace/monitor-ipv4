using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using monitor_ip_4_tool.Caching;
using monitor_ip_4_tool.Database;
using monitor_ip_4_tool.Interfaces;
using Microsoft.Extensions.Hosting;
using monitor_ip_4_tool.Constant;
using monitor_ip_4_tool.Serivces;
using Serilog;
using Polly;
using monitor_ip_4_tool.Models;
using Microsoft.Extensions.Options;

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
    public MyBackGroundService(
            IOpenVPN openVPN,
            ICaching memoryCache,
            IDatabase database,
            ILog logger,
            IEnumerable<IInternetProtocol> ipv4Services,
            ISendMail smtpService,
            IRetryHandler retryHandler,
            IPollyFactory pollyFactory,
            IOptionsMonitor<SystemConfig> systemConfigMonitor
            )
    {
        _openVPN = openVPN;
        _systemConfigMonitor = systemConfigMonitor;
        _pipeline = pollyFactory.GetIPServicesPipeLine();
        _retryHandler = retryHandler;
        _memoryCache = memoryCache;
        _database = database;
        _logger = logger;
        _ipv4Services = ipv4Services;
        _smtpService = smtpService;

        _systemConfigMonitor.OnChange((config) =>
        {
            _logger.Info($"System change config {config.ToStringJson()}");
        });


    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            await Task.Delay(_systemConfigMonitor.CurrentValue.ScanIPv4FromSecond, stoppingToken);
            try
            {
                string ipFromService = await _pipeline.ExecuteAsync<string>(async (token) =>
                {
                    string ipv4 = String.Empty;
                    foreach (var service in _ipv4Services)
                    {
                        try
                        {
                            ipv4 = await service.GetIP4Async(token);
                            if (!String.IsNullOrEmpty(ipv4)) return ipv4;
                        }

                        catch (Exception ex)
                        {
                            _logger.Error($"Error IPv4 service: {ex.Message}");
                            Thread.Sleep(ThreadSleep.MONITOR_IP * 1000);
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
                await _database.ConnectDb();

                await _retryHandler.ExecuteAsync((token) => _smtpService.SendMail(token, subject: "IP has changed", body: ipFromService));
                _memoryCache.Set(Cachekeys.LAST_IP, ipFromService, null);
                await _database.SaveIP(ipFromService);
                await _database.CloseDb();
                await _openVPN.RestartService(_systemConfigMonitor.CurrentValue.LinuxOpenVPNService);
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
                    .UseSerilog()
                    .ConfigureAppConfiguration((context, config) =>
                        {
                            var env = context.HostingEnvironment.EnvironmentName;
                            config.AddJsonFile($"appsettings.Development.json", optional: false, reloadOnChange: true);
                            if (env == Constant.Environment.PROD)
                            {
                                config.AddJsonFile($"appsettings.Production.json", optional: false, reloadOnChange: true);
                            }
                        })
                    .ConfigureServices((context, services) =>
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

                           services.AddOptions<SystemConfig>().Bind(context.Configuration.GetSection(ConfigEnum.SYSTEM)).ValidateDataAnnotations().ValidateOnStart();
                           services.AddOptions<RedisConfig>().Bind(context.Configuration.GetSection(ConfigEnum.REDIS));
                           services.AddOptions<SMTPConfig>().Bind(context.Configuration.GetSection(ConfigEnum.SMTP));
                           services.AddSingleton<LinuxOpenVPNService>();
                           services.AddSingleton<WindowOpenVPNService>();

                           services.AddSingleton<IOpenVPN>(sp =>
                               OperatingSystem.IsLinux()
                                   ? sp.GetRequiredService<LinuxOpenVPNService>()
                                   : sp.GetRequiredService<WindowOpenVPNService>()
                           );

                           services.AddHostedService<MyBackGroundService>();

                       }).Build();
            await host.RunAsync();
        }
    }
}