using System.ServiceProcess;

using Microsoft.Extensions.Options;

using Shared.Shared.Common.Interfaces;
using Shared.Shared.Common.Models;

namespace Shared.Shared.Infrastructure.Serivces
{
    public class WindowOpenVPNService : IOpenVPN
    {
        private const int timeoutMilliseconds = 100000;
        private readonly IOptionsMonitor<SystemConfig> _systemConfigMonitor;
        private OperatingConfig _operatingConfig;

        private readonly ILog _logger;
        public WindowOpenVPNService(
            ILog logger,
            IOptionsMonitor<SystemConfig> systemConfigMonitor

            )
        {
            _systemConfigMonitor = systemConfigMonitor;
            _logger = logger;
        }

        public async Task RestartService()
        {
            var serviceName = OperatingSystem.IsLinux() ?
                    _systemConfigMonitor.CurrentValue.LinuxOperating.OpenVPNService :
                    _systemConfigMonitor.CurrentValue.WindowOperating.OpenVPNService;
            await Task.Run(() =>
            {
#pragma warning disable CA1416

                ServiceController service = new ServiceController(serviceName);
                TimeSpan timeout = TimeSpan.FromMilliseconds(timeoutMilliseconds);

                if (service.Status != ServiceControllerStatus.Stopped &&
                    service.Status != ServiceControllerStatus.StopPending)
                {
                    service.Stop();
                    service.WaitForStatus(ServiceControllerStatus.Stopped, timeout);
                }
                service.Start();
                service.WaitForStatus(ServiceControllerStatus.Running, timeout);

                _logger.Info($"Restart Service Openvpn Window Successful at {DateTime.Now}");

#pragma warning restore CA1416

            });

        }

        public async Task UpdateClient(string ipv4)
        {

            await Task.Run(() =>
           {
               _operatingConfig = _systemConfigMonitor.CurrentValue.WindowOperating;
               try
               {
                   Func<string, string> GetAppPath = (nameFile) =>
                   {
                       return Path.Combine(AppContext.BaseDirectory, nameFile);
                   };

                   var caContent = File.ReadAllText(GetAppPath("ca.txt"));
                   var certContent = File.ReadAllText(GetAppPath("cert.txt"));
                   var keyContent = File.ReadAllText(GetAppPath("key.txt"));
                   var clientConfig = File.ReadAllText(GetAppPath("client.ovpn"));

                   _logger.Info($"App Path: {GetAppPath("ca.txt")}");

                   var newClientConfig = clientConfig.Replace("{ipv4}", ipv4)
                                                     .Replace("{ca}", caContent)
                                                     .Replace("{cert}", certContent)
                                                     .Replace("{directory}", _operatingConfig.DirectoryOpenVPNAccount)
                                                     .Replace("{key}", keyContent);
                   File.WriteAllText($"{_operatingConfig.DirectoryOpenVPNConfig}", newClientConfig);
                   _logger.Info($"Write myconfig.conf successfull at {_operatingConfig.DirectoryOpenVPN}");


                   var accountClient = File.ReadAllText(GetAppPath("password.txt"));
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
