using System.Net;
using System.Net.Sockets;
using monitor_ip_4_tool.Interfaces;

namespace monitor_ip_4_tool.Serivces;

public class IfConfigServices : IInternetProtocol
{
    private const string url = "https://ifconfig.me/ip";

    private readonly HttpClient _httpClient;

    private readonly ILog _logger;

    public IfConfigServices(
        ILog logger,
        ICustomHttpFactory httpClient

        )
    {
        _logger = logger;
        _httpClient = httpClient.GetIPv4Client();

    }

    public async Task<string> GetIP4Async(CancellationToken token = default)
    {
        _logger.Info("Send Request IfConfig!");
        var response = await _httpClient.GetStringAsync(url, token);
        _logger.Info($"Ifconfigme: {response}");
        if (String.IsNullOrEmpty(response)) return String.Empty;

        if (IPAddress.TryParse(response, out var ip) && ip.AddressFamily == AddressFamily.InterNetwork)
            return ip.ToString();
        _logger.Error($"Invalid IP Address: {response}");
        return String.Empty;
    }
}