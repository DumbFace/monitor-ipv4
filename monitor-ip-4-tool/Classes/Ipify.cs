using System.Net;
using System.Net.Sockets;
using monitor_ip_4_tool.Interfaces;
using monitor_ip_4_tool.Models;
using Newtonsoft.Json;
using Serilog;

namespace monitor_ip_4_tool.Classes;

public class Ipify : IInternetProtocol
{
    private const string url = "https://ipinfo.io/json";

    private static readonly HttpClient Http = new(new HttpClientHandler { AllowAutoRedirect = true })
    {
        Timeout = TimeSpan.FromSeconds(10)
    };
    private readonly ILog _logger;
    public Ipify(ILog logger)
    {
        _logger = logger;
        Http.DefaultRequestHeaders.UserAgent.ParseAdd("curl/7.64.1");
    }

    public async Task<string> GetIP4Async()
    {
        try
        {
            var response = (await Http.GetStringAsync(url)).Trim();

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