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

namespace monitor_ip_4_tool;

public class MyBackGroundService : BackgroundService
{
    private readonly ICaching _memoryCache;
    private readonly IDatabase _database;
    private readonly ILog _logger;
    private readonly IInternetProtocol _ifconfig;
    private readonly IInternetProtocol _ipify;
    private readonly ISendMail _smtpService;
    private readonly ResiliencePipelineProvider<string> _polly;
    private readonly IHttpClientFactory _httpClient;

    public MyBackGroundService(
            ICaching memoryCache,
            IDatabase database,
            ILog logger,
            IInternetProtocol ifconfig,
            IInternetProtocol ipify,
            ISendMail smtpService,
            ResiliencePipelineProvider<string> polly,
            IHttpClientFactory httpClient
            )
    {
        _memoryCache = memoryCache;
        _database = database;
        _logger = logger;
        _ifconfig = ifconfig;
        _ipify = ipify;
        _smtpService = smtpService;
        _polly = polly;
        _httpClient = httpClient;

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
                // using (HttpClient client = new HttpClient())
                // {
                //     client.Timeout = TimeSpan.FromSeconds(5);
                //     // var response = await client.GetAsync("http://medium.com/");
                //     var response = await client.GetAsync("https://www.gaoogle.com/");

                //     response.EnsureSuccessStatusCode();

                //     if (response.StatusCode == HttpStatusCode.OK)
                //     {
                //         _logger.Info("Request OK ");

                //     }
                // }

                var pipeline = _polly.GetPipeline("my-pipeline");
                await pipeline.ExecuteAsync(async token =>
                {

                    var client = _httpClient.CreateClient("myClient");

                    var response = await client.GetAsync("https://www.gaoogle.com/");
                    if (response.StatusCode == HttpStatusCode.OK)
                        _logger.Info("Request Success!!");

                    // _logger.Info("Retry");
                    // return default;
                    // return Task.CompletedTask;
                });

                continue;
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
                var circuitOptions = new CircuitBreakerStrategyOptions
                {
                    FailureRatio = 0.5,
                    SamplingDuration = TimeSpan.FromSeconds(30),
                    MinimumThroughput = 10,
                    BreakDuration = TimeSpan.FromSeconds(15),
                };
                services.AddResiliencePipeline("my-pipeline", pipeline =>
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
                    pipeline.AddTimeout(TimeSpan.FromSeconds(5));
                    pipeline.AddCircuitBreaker(circuitOptions);
                });
                services.AddSingleton<ILog, LogServices>();
                services.AddSingleton<IConfiguration>(context.Configuration);
                services.AddSingleton<ISendMail, SMTPService>();
                services.AddSingleton<IConfigApp, SMTPConfigService>();
                services.AddHttpClient("myClient", client =>
                {
                    client.Timeout = Timeout.InfiniteTimeSpan;
                });
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