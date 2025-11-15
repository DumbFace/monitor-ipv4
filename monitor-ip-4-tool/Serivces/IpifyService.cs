using System.Net;
using System.Net.Sockets;
using monitor_ip_4_tool.Interfaces;
using monitor_ip_4_tool.Models;
using Newtonsoft.Json;

namespace monitor_ip_4_tool.Serivces;

public class IpifyService : IInternetProtocol
{
    private const string url = "https://ipinfo.io/json";
    private readonly IHttpClientFactory _http;

    private readonly ILog _logger;

    public IpifyService(ILog logger, IHttpClientFactory http)
    {
        _logger = logger;
        _http = http;
    }

    public async Task<string> GetIP4Async(CancellationToken token = default)
    {
        //TODO Delete later
        _logger.Info("Before Delay");
        await Task.Delay(1000, token);
        _logger.Info("After Delay");
        var client = _http.CreateClient("httpClient");
        _logger.Info("Send Request Ipify!");
        var response = (await client.GetStringAsync(url, token)).Trim();
        _logger.Info($"{response}");

        var ipAsString = JsonConvert.DeserializeObject<IpInfoModel>(response).Ip;
        if (String.IsNullOrEmpty(ipAsString)) return String.Empty;

        if (IPAddress.TryParse(ipAsString, out var ip) && ip.AddressFamily == AddressFamily.InterNetwork)
            return ip.ToString();
        _logger.Error($"Invalid IP Address: {response}");

        return String.Empty;
    }
}