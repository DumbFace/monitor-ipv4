using System.Diagnostics;

using Microsoft.Extensions.Options;

using Shared.Shared.Common.Constant;
using Shared.Shared.Common.Interfaces;
using Shared.Shared.Common.Models;

namespace Shared.Shared.Infrastructure.Serivces
{
    public class LinuxWireguardService : IWireguard
    {
        private readonly string WG_SECRET = Environment.GetEnvironmentVariable(EnvironmentEnum.WG_SECRET);
        private readonly ILog _logger;
        private readonly IOptionsMonitor<WireguardVPNConfig> _optionsMonitorWireguard;

        private WireguardVPNConfig _wireguardVPNConfig;
        public LinuxWireguardService(
            ILog logger,
            IOptionsMonitor<WireguardVPNConfig> optionsMonitorWireguard
            )
        {
            _optionsMonitorWireguard = optionsMonitorWireguard;
            _logger = logger;
        }

        public async Task RestartService()
        {
            await Task.Run(() => Process.Start("sudo", $"systemctl restart {_optionsMonitorWireguard.CurrentValue.VPNService}"));
            _logger.Info($"Restart Service wireguard vpn linux successful at {DateTime.Now}");
        }

        public async Task UpdateClient(string ipv4)
        {
            await Task.Run(() =>
            {
                _wireguardVPNConfig = _optionsMonitorWireguard.CurrentValue;
                try
                {
                    Func<string, string> GetAppPath = (nameFile) =>
                    {
                        return Path.Combine(AppContext.BaseDirectory, nameFile);
                    };

                    var wireguardConfig = File.ReadAllText(GetAppPath("wg0.txt"));

                    var newClientConfig = wireguardConfig
                                                      .Replace("{ipv4}", ipv4)
                                                      .Replace("{address}", _wireguardVPNConfig.Address)
                                                      .Replace("{privateKey}", WG_SECRET)
                                                      .Replace("{publicKey}", _wireguardVPNConfig.PublicKey)
                                                      .Replace("{subnetVPN}", _wireguardVPNConfig.SubnetVPN);

                    File.WriteAllText($"{_wireguardVPNConfig.VPNConfigDirectory}", newClientConfig);
                    _logger.Info($"Write wireguard.conf successfull at {_wireguardVPNConfig.VPNConfigDirectory}");
                }

                catch (Exception ex)
                {
                    _logger.Info($"Msg: {ex.Message}");
                }
            });
        }
    }
}
