using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using monitor_ip_4_tool.Caching;
using monitor_ip_4_tool.Database;
using monitor_ip_4_tool.Interfaces;
using monitor_ip_4_tool.Utils;
using Microsoft.Extensions.Hosting;
using monitor_ip_4_tool.Constant;
using monitor_ip_4_tool.Serivces;
using Serilog;

namespace monitor_ip_4_tool;

public class MyBackGroundService : BackgroundService
{
    private readonly ICaching _memoryCache;
    private readonly IDatabase _database;
    private readonly ILog _logger;
    private readonly IInternetProtocol _ifconfig;
    private readonly IInternetProtocol _ipify;
    private readonly ISendMail _smtpService;


    public MyBackGroundService(ICaching memoryCache, IDatabase database, ILog logger, IInternetProtocol ifconfig,
        IInternetProtocol ipify, ISendMail smtpService)
    {
        _memoryCache = memoryCache;
        _database = database;
        _logger = logger;
        _ifconfig = ifconfig;
        _ipify = ipify;
        _smtpService = smtpService;
    }

    public async Task<string> GetIPv4(IEnumerable<IInternetProtocol> services)
    {
        string ip = String.Empty;
        foreach (var task in services)
        {
            ip = await task.GetIP4Async();
            if (!String.IsNullOrEmpty(ip)) return ip;
        }

        return ip;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            Thread.Sleep(ThreadSleep.MONITOR_IP * 1000);

            try
            {
                IEnumerable<IInternetProtocol> services = new List<IInternetProtocol>() { _ifconfig, _ipify };
                var ipFromService = await GetIPv4(services);
                if (String.IsNullOrEmpty(ipFromService))
                {
                    _logger.Info($"It is null or empty ip services: {ipFromService}");
                    continue;
                }

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

                throw new Exception("Fake error");
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
            .ConfigureServices((context, services) =>
            {
                services.AddSingleton<ILog, LogServices>();
                services.AddSingleton<IConfiguration>(context.Configuration);
                services.AddSingleton<ISendMail, SMTPService>();
                services.AddSingleton<IConfigApp, SMTPConfigService>();

                //Alternative redis caching 
                services.AddSingleton<ICaching, RedisCacheService>();
                services.AddSingleton<ICaching, MicrosoftMemoryCacheService>();
                services.AddSingleton<IInternetProtocol, IfConfigServices>();
                services.AddSingleton<IInternetProtocol, IpifyService>();
                services.AddSingleton<IDatabase, SqlLite>();
                services.AddHostedService<MyBackGroundService>();
            }).Build();
            await host.RunAsync();
        }
    }
}