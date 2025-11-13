using System.Net;
using System.Net.Sockets;
using Microsoft.Extensions.Caching.Memory;
using monitor_ip_4_tool.Interfaces;
using Serilog;

namespace monitor_ip_4_tool.Serivces;

public class IfConfigServices : IInternetProtocol
{
    private const string url = "https://ifcowdwkdnwkd1dnfig.me/ip";

    private readonly IHttpClientFactory _httpClient;

    private readonly ILog _logger;

    public IfConfigServices(
        ILog logger,
        IHttpClientFactory httpClient

        )
    {
        _logger = logger;
        _httpClient = httpClient;

    }

    public async Task<string> GetIP4Async()
    {
        try
        {
            var client = _httpClient.CreateClient("httpClient");
            _logger.Info("Send Request IfConfig!");
            var response = (await client.GetStringAsync(url)).Trim();
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