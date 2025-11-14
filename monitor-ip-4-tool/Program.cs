using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using monitor_ip_4_tool.Caching;
using monitor_ip_4_tool.Database;
using monitor_ip_4_tool.Interfaces;
using Microsoft.Extensions.Hosting;
using monitor_ip_4_tool.Constant;
using monitor_ip_4_tool.Serivces;
using Serilog;
using Polly.Caching;

namespace monitor_ip_4_tool;

public class MyBackGroundService : BackgroundService
{
    private readonly ICaching _memoryCache;
    private readonly IDatabase _database;
    private readonly ILog _logger;
    private readonly IInternetProtocol _ifconfig;
    private readonly IInternetProtocol _ipify;
    private readonly ISendMail _smtpService;
    private readonly IRetryHandler _retryHandler;


    public MyBackGroundService(
            ICaching memoryCache,
            IDatabase database,
            ILog logger,
            IInternetProtocol ifconfig,
            IInternetProtocol ipify,
            ISendMail smtpService,
            IRetryHandler retryHandler
            )
    {
        _retryHandler = retryHandler;
        _memoryCache = memoryCache;
        _database = database;
        _logger = logger;
        _ifconfig = ifconfig;
        _ipify = ipify;
        _smtpService = smtpService;

    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            Thread.Sleep(ThreadSleep.MONITOR_IP * 1000);

            try
            {

                var ipFromService = await _retryHandler.ExecuteAsync(_ifconfig.GetIP4Async);

                //TODO Make Chaining method
                ipFromService = String.IsNullOrEmpty(ipFromService) ? await _retryHandler.ExecuteAsync(_ipify.GetIP4Async) : ipFromService;

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
                    _memoryCache.Set<string>(Cachekeys.LAST_IP, ipFromDb, null);
                    _logger.Info($"Ip from db:  {ipFromDb}");

                }
                _logger.Info($"Ip from caching:  {ipFromCaching}");
                _logger.Info($"Ip from service:  {ipFromService}");
                _logger.Info($"Ip from lastIp:  {lastIp}");

                if (lastIp == ipFromService)
                    continue;
                await _database.ConnectDb();

                _memoryCache.Set<string>(Cachekeys.LAST_IP, ipFromService, null);
                await _database.SaveIP(ipFromService);
                await _smtpService.SendMail(subject: "IP has changed", body: ipFromService);
                await _database.CloseDb();

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
                // var directory = 
                config.AddJsonFile($"appsettings.Development.json", optional: false, reloadOnChange: true);
                if (env == Constant.Environment.PROD)
                {
                    config.AddJsonFile($"appsettings.Production.json", optional: false, reloadOnChange: true);
                }
            })
            .ConfigureServices((context, services) =>
                    {
                        services.AddSingleton<ILog, LogServices>();

                        services.AddHttpClient("httpClient", client =>
                        {
                            client.Timeout = Timeout.InfiniteTimeSpan;
                        });

                        services.AddSingleton<IPollyFactory, PollyFactory>();
                        services.AddSingleton<IRetryHandler, RetryServices>();

                        services.AddSingleton<IConfiguration>(context.Configuration);
                        services.AddSingleton<ISendMail, SMTPService>();
                        services.AddSingleton<IConfigApp, SMTPConfigService>();

                        //Alternative redis caching 
                        services.AddSingleton<ICaching, RedisCacheService>();
                        // services.AddSingleton<ICaching, MicrosoftMemoryCacheService>();
                        services.AddSingleton<IInternetProtocol, IfConfigServices>();
                        services.AddSingleton<IInternetProtocol, IpifyService>();
                        services.AddSingleton<IDatabase, SqlLite>();
                        services.AddHostedService<MyBackGroundService>();
                    }).Build();
            await host.RunAsync();
        }
    }
}