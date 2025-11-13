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
using Polly;
using Polly.Retry;
using Polly.CircuitBreaker;
using Polly.Registry;
using static System.Net.WebRequestMethods;
using System.Net;
using Polly.Timeout;
using SQLitePCL;

namespace monitor_ip_4_tool;

public class MyBackGroundService : BackgroundService
{
    private readonly ICaching _memoryCache;
    private readonly IDatabase _database;
    private readonly ILog _logger;
    private readonly IInternetProtocol _ifconfig;
    // private readonly IInternetProtocol _ipify;
    private readonly ISendMail _smtpService;
    private readonly IRetryHandler _retryHandler;

    private readonly IHttpClientFactory http;

    public MyBackGroundService(
            ICaching memoryCache,
            IDatabase database,
            ILog logger,
            IInternetProtocol ifconfig,
            // IInternetProtocol ipify,
            ISendMail smtpService,
            IRetryHandler retryHandler,
            IHttpClientFactory httpClient
            )
    {
        http = httpClient;
        _retryHandler = retryHandler;
        _memoryCache = memoryCache;
        _database = database;
        _logger = logger;
        _ifconfig = ifconfig;
        // _ipify = ipify;
        _smtpService = smtpService;

    }

    // public async Task<string> GetIPv4(IEnumerable<IInternetProtocol> services)
    // {
    //     string ip = String.Empty;
    //     foreach (var task in services)
    //     {
    //         ip = await task.GetIP4Async();
    //         if (!String.IsNullOrEmpty(ip)) return ip;
    //     }

    //     return ip;
    // }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            Thread.Sleep(ThreadSleep.MONITOR_IP * 1000);
            var client = http.CreateClient("httpClient");

            try
            {
                // IEnumerable<IInternetProtocol> services = new List<IInternetProtocol>() { _ifconfig, _ipify };
                // var ipFromService = await _retryHandler.ExecuteAsync<string>(async () => { return await _ifconfig.GetIP4Async(); });
                var ipFromService = await _retryHandler.ExecuteAsync<string>(async () =>
                {
                    // var response = await client.GetAsync("goowdwddwgle.com");
                    // Task.Delay(5000);
                    return "Done";
                });
                _logger.Info(ipFromService);
                // var ipFromService = await GetIPv4(services);
                // if (String.IsNullOrEmpty(ipFromService))
                // {
                //     _logger.Info($"It is null or empty ip services: {ipFromService}");
                //     continue;
                // }
                // _logger.Info($"{ipFromService}");

                // var ipFromCaching = _memoryCache.Get<string>(Cachekeys.LAST_IP);
                // var lastIp = ipFromCaching;
                // if (String.IsNullOrEmpty(ipFromCaching))
                // {
                //     await _database.ConnectDb();
                //     var ipFromDb = await _database.GetLastIP();
                //     if (String.IsNullOrEmpty(ipFromDb))
                //     {
                //         await _database.InitDb();
                //         lastIp = IP.LOCALIP;
                //     }
                //     lastIp = ipFromDb;
                //     _memoryCache.Set<string>(Cachekeys.LAST_IP, ipFromDb, null);
                //     _logger.Info($"Ip from db:  {ipFromDb}");

                // }
                // _logger.Info($"Ip from caching:  {ipFromCaching}");
                // _logger.Info($"Ip from service:  {ipFromService}");
                // _logger.Info($"Ip from lastIp:  {lastIp}");

                // if (lastIp == ipFromService)
                //     continue;
                // await _database.ConnectDb();

                // _memoryCache.Set<string>(Cachekeys.LAST_IP, ipFromService, null);
                // await _database.SaveIP(ipFromService);
                // await _smtpService.SendMail(subject: "IP has changed", body: ipFromService);
                // await _database.CloseDb();

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
                        services.AddHttpClient("httpClient", client =>
                        {
                            client.Timeout = Timeout.InfiniteTimeSpan;
                        });
                        services.AddResiliencePipeline("defaultPipeline", pipeline =>
                        {
                            pipeline.AddRetry(new RetryStrategyOptions
                            {
                                MaxRetryAttempts = 3,
                                Delay = TimeSpan.FromSeconds(5),
                                OnRetry = (args) =>
                                {
                                    Console.WriteLine($"Retry attempt {args.AttemptNumber} after {args.RetryDelay}s due to {args.Outcome.Exception.Message}");
                                    return default;
                                },

                            });
                            pipeline.AddTimeout(new TimeoutStrategyOptions
                            {
                                Timeout = TimeSpan.FromSeconds(2),
                                OnTimeout = (args) =>
                                {
                                    Console.WriteLine($"event on timeout");
                                    return default;
                                }
                            });
                        });

                        services.AddSingleton<ILog, LogServices>();
                        services.AddSingleton<IConfiguration>(context.Configuration);
                        services.AddSingleton<ISendMail, SMTPService>();
                        services.AddSingleton<IConfigApp, SMTPConfigService>();
                        services.AddSingleton<IRetryHandler, RetryServices>();
                        //Alternative redis caching 
                        services.AddSingleton<ICaching, RedisCacheService>();
                        services.AddSingleton<ICaching, MicrosoftMemoryCacheService>();
                        services.AddSingleton<IInternetProtocol, IfConfigServices>();
                        // services.AddSingleton<IInternetProtocol, IpifyService>();
                        services.AddSingleton<IDatabase, SqlLite>();
                        services.AddHostedService<MyBackGroundService>();
                    }).Build();
            await host.RunAsync();
        }
    }
}