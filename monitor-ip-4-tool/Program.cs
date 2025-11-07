using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using monitor_ip_4_tool.Caching;
using monitor_ip_4_tool.Classes;
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


    public MyBackGroundService(ICaching memoryCache, IDatabase database, ILog logger, IInternetProtocol ifconfig,
        IInternetProtocol ipify)
    {
        _memoryCache = memoryCache;
        _database = database;
        _logger = logger;
        _ifconfig = ifconfig;
        _ipify = ipify;
    }

    public async Task<string> GetIPv4(IEnumerable<IInternetProtocol> services)
    {
        // var tasks = services.Select(s => s.GetIP4Async());
        string ip = String.Empty;
        foreach (var task in services)
        {
            ip = await task.GetIP4Async();
            Thread.Sleep(TimeSpan.FromSeconds(5));
            if (!String.IsNullOrEmpty(ip)) return ip;
        }

        return ip;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        bool initDbOnce = false;
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                _database.ConnectDb();
                if (!initDbOnce)
                {
                    _database.InitDb();
                    initDbOnce = true;
                }

                IEnumerable<IInternetProtocol> services = new List<IInternetProtocol>() { _ifconfig };
                var ipFromService = await GetIPv4(services);
                // var ipFromService = await _ifconfig.GetIP4Async();
                var ipFromCaching = _memoryCache.Get<string>(Cachekeys.LAST_IP);
                Console.WriteLine($"ipFromService:  {ipFromService}");
                Console.WriteLine($"ipFromCaching:  {ipFromCaching}");
                if (String.IsNullOrEmpty(ipFromService)) continue;
                if (String.IsNullOrEmpty(ipFromCaching))
                {
                    var ipSql = await _database.GetLastIP();
                    _memoryCache.Set<string>(Cachekeys.LAST_IP, ipSql, null);
                    Console.WriteLine($"ipSql:  {ipSql}");
                }
                else
                {
                    if (ipFromCaching != ipFromService)
                    {
                        _memoryCache.Set<string>(Cachekeys.LAST_IP, ipFromService, null);
                        await _database.SaveIP(ipFromService);
                        //TODO Send Mail
                        _logger.Info($"Send Email Or Sync New IP: ${ipFromService}");
                        Console.WriteLine($"Send Mail");
                    }
                }

                await _database.CloseDb();
            }
            catch (Exception ex)
            {
                _logger.Error($"Error: {ex.Message}");
            }

            await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
        }
    }
}

public class Program
{
    private static async Task Main(string[] args)
    {
        using IHost host = Host.CreateDefaultBuilder(args).ConfigureServices(services =>
        {
            // services.AddScoped<IMemoryCache, >();

            services.AddSingleton<ILog, LogServices>();
            services.AddSingleton<IDatabase, SqlLite>();
            services.AddSingleton<ICaching, RedisCache>();
            // services.AddSingleton<ICaching, MicrosoftMemoryCache>();
            services.AddSingleton<IInternetProtocol, IfConfig>();
            services.AddSingleton<IInternetProtocol, Ipify>();
            services.AddHostedService<MyBackGroundService>();
        }).Build();
        await host.RunAsync();
    }
}