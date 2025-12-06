using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

using Serilog;

using Shared.Shared.Common.Constant;
using Shared.Shared.Common.Interfaces;
using Shared.Shared.Common.Models;
using Shared.Shared.Infrastructure.Caching;
using Shared.Shared.Infrastructure.Database;
using Shared.Shared.Infrastructure.Serivces;

namespace monitor_ipv4_client;

public class Program
{
    private static async Task Main(string[] args)
    {
        using IHost host = Host.CreateDefaultBuilder(args).UseSerilog().UseWindowsService()
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
            .ConfigureServices(static (context, services) =>
            {

                services.AddLogging((logger) =>
                    {
                        logger.ClearProviders();
                        logger.AddSerilog();
                    });
                services.AddSingleton<ILog, LogServices>();
                services.AddSingleton<ILog, LogServices>();

                services.AddSingleton<IPollyFactory, PollyFactory>();
                services.AddSingleton<IRetryHandler, RetryServices>();

                services.AddSingleton(context.Configuration);

                services.AddSingleton<ICaching, MicrosoftMemoryCacheService>();
                services.AddSingleton<IDatabase, SqlLite>();
                services.AddSingleton<ICustomHttpFactory, CustomHttpClientFactory>();

                services.AddOptions<SystemConfig>().Bind(context.Configuration.GetSection(ConfigEnum.SYSTEM))
                    .ValidateDataAnnotations().ValidateOnStart();
                services.AddOptions<WireguardVPNConfig>().Bind(context.Configuration.GetSection(ConfigEnum.WIREGUARD));

                services.AddSingleton<IVPNHandler, LinuxWireguardService>();
                services.AddSingleton<IDataCRUD, Firebase>();

                // ! Do not remove if you use openvpn 
                // services.AddSingleton<LinuxOpenVPNService>();
                // services.AddSingleton<WindowOpenVPNService>();

                // services.AddSingleton<IVPNHandler>(sp =>
                //     OperatingSystem.IsLinux()
                //         ? sp.GetRequiredService<LinuxOpenVPNService>()
                //         : sp.GetRequiredService<WindowOpenVPNService>());

                services.AddHostedService<MonitorIpv4ClientService>();
            }).Build();
        await host.RunAsync();
    }
}

