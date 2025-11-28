using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

using Polly;

using Shared.Shared.Common.Constant;
using Shared.Shared.Common.Interfaces;
using Shared.Shared.Common.Models;

namespace Shared.Shared.Infrastructure.Serivces;

public class MonitorIpv4ClientService : BackgroundService
{
    private readonly ICaching _memoryCache;
    private readonly IDatabase _database;

    private readonly IOpenVPN _openVPN;
    readonly IOptionsMonitor<SystemConfig> _systemConfigMonitor;
    private readonly ResiliencePipeline _pipeline;

    private readonly ILog _logger;
    private readonly IDataCRUD _firebase;

    public MonitorIpv4ClientService(
        IDataCRUD firebase,
        IOpenVPN openVPN,
        ICaching memoryCache,
        IDatabase database,
        ILog logger,
        IPollyFactory pollyFactory,
        IOptionsMonitor<SystemConfig> systemConfigMonitor
    )
    {
        _firebase = firebase;
        _systemConfigMonitor = systemConfigMonitor;
        _pipeline = pollyFactory.GetIPServicesPipeLine();
        _memoryCache = memoryCache;
        _database = database;
        _logger = logger;
        _openVPN = openVPN;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            await Task.Delay(_systemConfigMonitor.CurrentValue.ScanIPv4FromSecond, stoppingToken);
            try
            {
                string ipFromService = await _pipeline.ExecuteAsync(async (token) =>
                {
                    return await _firebase.GetLastIP(token);
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

                await _openVPN.UpdateClient(ipFromService);
                await _openVPN.RestartService(OperatingSystem.IsLinux() ?
                    _systemConfigMonitor.CurrentValue.LinuxOperating.OpenVPNService :
                    _systemConfigMonitor.CurrentValue.WindowOperating.OpenVPNService);

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
}
