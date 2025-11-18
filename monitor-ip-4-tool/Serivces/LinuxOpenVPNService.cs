
using System.Diagnostics;
using Microsoft.Extensions.Options;
using monitor_ip_4_tool.Constant;
using monitor_ip_4_tool.Interfaces;
using monitor_ip_4_tool.Models;

namespace monitor_ip_4_tool.Serivces
{
    public class LinuxOpenVPNService : IOpenVPN
    {
        private readonly ILog _logger;
        private readonly IOptionsMonitor<SystemConfig> _systemConfigMonitor;

        private OperatingConfig _operatingConfig;
        public LinuxOpenVPNService(
            ILog logger,
            IOptionsMonitor<SystemConfig> systemConfigMonitor
            )
        {
            _systemConfigMonitor = systemConfigMonitor;
            _logger = logger;
        }

        public async Task RestartService(string serviceName)
        {
            await Task.Run(() => Process.Start("sudo", $"systemctl restart {serviceName}"));
            _logger.Info($"Restart Service Openvpn Linux Successful at {DateTime.Now}");
        }

        public async Task UpdateClient(string ipv4)
        {
            await Task.Run(() =>
            {
                _operatingConfig = _systemConfigMonitor.CurrentValue.LinuxOperating;
                try
                {

                    var caContent = File.ReadAllText("ca.txt");
                    var certContent = File.ReadAllText("cert.txt");
                    var keyContent = File.ReadAllText("key.txt");

                    var clientConfig = File.ReadAllText("client.ovpn");
                    var newClientConfig = clientConfig.Replace("{ipv4}", ipv4)
                                                      .Replace("{ca}", caContent)
                                                      .Replace("{cert}", certContent)
                                                      .Replace("{directory}", _operatingConfig.DirectoryOpenVPNAccount)
                                                      .Replace("{key}", keyContent);
                    File.WriteAllText($"{_operatingConfig.DirectoryOpenVPNConfig}", newClientConfig);
                    _logger.Info($"Write myconfig.conf successfull at {_operatingConfig.DirectoryOpenVPN}");


                    var accountClient = File.ReadAllText("password.txt");
                    var newAccountClient = accountClient.Replace("{username}", _operatingConfig.UserName)
                                                          .Replace("{password}", _operatingConfig.Password);

                    File.WriteAllText($"{_operatingConfig.DirectoryOpenVPNAccount}", newAccountClient);
                    _logger.Info($"Write password.txt successfull at {_operatingConfig.DirectoryOpenVPN}");
                }

                catch (Exception ex)
                {
                    _logger.Info($"Msg: {ex.Message}");
                }
            });
        }
    }
}