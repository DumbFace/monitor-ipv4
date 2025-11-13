using System.Net;
using System.Net.Sockets;
using monitor_ip_4_tool.Interfaces;
using monitor_ip_4_tool.Models;
using Newtonsoft.Json;
using Serilog;

namespace monitor_ip_4_tool.Serivces;

public class IpifyService : IInternetProtocol
{
    private const string url = "https://ipiwdnfo.io/json";
    private readonly IHttpClientFactory _http;

    private readonly ILog _logger;

    public IpifyService(ILog logger, IHttpClientFactory http)
    {
        _logger = logger;
        _http = http;
    }

    public async Task<string> GetIP4Async()
    {
        try
        {
            var client = _http.CreateClient("httpClient");
            _logger.Info("Send Request Ipify!");
            var response = (await client.GetStringAsync(url)).Trim();

            var ipAsString = JsonConvert.DeserializeObject<IpInfoModel>(response).Ip;
            if (String.IsNullOrEmpty(ipAsString)) return String.Empty;

            if (IPAddress.TryParse(ipAsString, out var ip) && ip.AddressFamily == AddressFamily.InterNetwork)
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