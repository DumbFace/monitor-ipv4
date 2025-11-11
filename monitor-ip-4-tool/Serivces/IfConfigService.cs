using System.Net;
using System.Net.Sockets;
using Microsoft.Extensions.Caching.Memory;
using monitor_ip_4_tool.Interfaces;
using Serilog;

namespace monitor_ip_4_tool.Serivces;

public class IfConfigServices : IInternetProtocol
{
    private const string url = "https://ifconfig.me/ip";

    private static readonly HttpClient Http = new(new HttpClientHandler { AllowAutoRedirect = true })
    {
        Timeout = TimeSpan.FromSeconds(10)
    };
    
    private readonly ILog _logger;

    public IfConfigServices(ILog logger)
    {
        _logger = logger;
        Http.DefaultRequestHeaders.UserAgent.ParseAdd("curl/7.64.1");
    }

    public async Task<string> GetIP4Async()
    {
        try
        {
            _logger.Info("Send Request IfConfig!");
            var response = (await Http.GetStringAsync(url)).Trim();
            if (String.IsNullOrEmpty(response)) return String.Empty;

            if (IPAddress.TryParse(response, out var ip) && ip.AddressFamily == AddressFamily.InterNetwork)
                return ip.ToString();
            _logger.Error($"Invalid IP Address: {response}");
        }
        catch (Exception ex)
        {
            _logger.Error($"message: {ex.Message}");
        }

        return String.Empty;
    }
}