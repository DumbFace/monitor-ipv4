
using System.ServiceProcess;
using monitor_ip_4_tool.Interfaces;

namespace monitor_ip_4_tool.Serivces
{
    public class WindowOpenVPNService : IOpenVPN
    {
        private const int timeoutMilliseconds = 100000;

        private readonly ILog _logger;
        public WindowOpenVPNService(ILog logger)
        {
            _logger = logger;
        }

        public async Task RestartService(string serviceName)
        {
            await Task.Run(() =>
            {
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
            });

        }

        public Task UpdateClient(string ipv4)
        {
            
            throw new NotImplementedException();
        }
    }
}