using System.Net;
using System.Net.Sockets;
using monitor_ip_4_tool.Interfaces;
using monitor_ip_4_tool.Models;
using Newtonsoft.Json;

namespace monitor_ip_4_tool.Serivces;

public class IpifyService : IInternetProtocol
{
    private const string url = "https://ipinfo.io/json";
    private readonly HttpClient _httpClient;
    private readonly ILog _logger;

    public IpifyService(ILog logger, ICustomHttpFactory http)
    {
        _logger = logger;
        _httpClient = http.GetIPv4Client();
    }

    public async Task<string> GetIP4Async(CancellationToken token = default)
    {
        _logger.Info("Send Request Ipify!");
        var response = await _httpClient.GetStringAsync(url, token);
        _logger.Info($"Ipify: {response}");

        var ipAsString = JsonConvert.DeserializeObject<IpInfoModel>(response).Ip;
        if (String.IsNullOrEmpty(ipAsString)) return String.Empty;

        if (IPAddress.TryParse(ipAsString, out var ip) && ip.AddressFamily == AddressFamily.InterNetwork)
            return ip.ToString();
        _logger.Error($"Invalid IP Address: {response}");

        return String.Empty;
    }
}