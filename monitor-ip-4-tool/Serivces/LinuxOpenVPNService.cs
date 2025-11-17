
using System.Diagnostics;
using monitor_ip_4_tool.Interfaces;

namespace monitor_ip_4_tool.Serivces
{
    public class LinuxOpenVPNService : IOpenVPN
    {
        private readonly ILog _logger;
        public LinuxOpenVPNService(ILog logger)
        {
            _logger = logger;
        }

        public async Task RestartService(string serviceName)
        {
            await Task.Run(() => Process.Start("sudo",$"systemctl restart {serviceName}"));
            _logger.Info($"Restart Service Openvpn Linux Successful at {DateTime.Now}");
        }
    }
}