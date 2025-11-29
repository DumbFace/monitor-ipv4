using System.Net;
using System.Net.Sockets;
using System.Text.Json.Nodes;

using Shared.Shared.Common.Interfaces;

namespace Shared.Shared.Common.Utils;

public static class InternetProtocolExtensions
{
    public static async Task<string> OrNextAsync(this Task<string> current, Func<Task<string>> next)
    {
        var result = await current;
        return string.IsNullOrEmpty(result) ? await next() : result;
    }

    public static string CheckingIpv4(this string ip, ILog _logger)
    {
        if (ip.TryParseJson())
        {
            var node = JsonNode.Parse(ip);
            ip = node["ip"]!.ToString();
        }


        if (IPAddress.TryParse(ip, out var ipv4Result) && ipv4Result.AddressFamily == AddressFamily.InterNetwork)
            return ipv4Result.ToString();
        _logger.Error($"Invalid IP Address: {ipv4Result}");
        return String.Empty;
    }

    public static bool TryParseJson(this string json)
    {
        try
        {
            var obj = System.Text.Json.JsonSerializer.Deserialize<object>(json);
            return obj != null;
        }
        catch
        {
            return false;
        }
    }
}
