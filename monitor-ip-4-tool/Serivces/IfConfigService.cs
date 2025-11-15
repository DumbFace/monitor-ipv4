using System.Net;
using System.Net.Sockets;
using monitor_ip_4_tool.Interfaces;

namespace monitor_ip_4_tool.Serivces;

public class IfConfigServices : IInternetProtocol
{
    private const string url = "https://ifconssfig.me/ip";

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

    public async Task<string> GetIP4Async(CancellationToken token = default)
    {
        //TODO Delete later
        _logger.Info("Before Delay");
        await Task.Delay(1000, token);
        _logger.Info("After Delay");
        var client = _httpClient.CreateClient("httpClient");
        _logger.Info("Send Request IfConfig!");
        var response = (await client.GetStringAsync(url, token)).Trim();
        if (String.IsNullOrEmpty(response)) return String.Empty;

        if (IPAddress.TryParse(response, out var ip) && ip.AddressFamily == AddressFamily.InterNetwork)
            return ip.ToString();
        _logger.Error($"Invalid IP Address: {response}");
        return String.Empty;
    }
}