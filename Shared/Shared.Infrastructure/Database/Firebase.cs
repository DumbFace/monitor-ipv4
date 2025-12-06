using System.Net;

using Newtonsoft.Json;

using Shared.Shared.Common.Constant;
using Shared.Shared.Common.Interfaces;

namespace Shared.Shared.Infrastructure.Database;

public class Firebase : IDataCRUD
{
    private const string UrlFirebase = "https://monitor-ipv4-f0afe-default-rtdb.asia-southeast1.firebasedatabase.app/{nameNode}.json?auth={secret}";
    private static readonly string FireBaseSecret = Environment.GetEnvironmentVariable(EnvironmentEnum.FIREBASE_SECRET);
    private readonly HttpClient _httpClient;

    private readonly ILog _logger;
    public Firebase(ILog logger, ICustomHttpFactory customHttp)
    {
        _logger = logger;
        _httpClient = customHttp.GetHttpClientDefault();
    }
    //TODO using JTW instead of firebase secret
    public async Task<string> GetLastIP(CancellationToken token = default)
    {
        var response = await _httpClient.GetStringAsync(BuildPath("IpLog"), token);
        _logger.Info($"Firebase response: {response}");
        _logger.Info($"Path: {BuildPath("IpLog")}");

        var ipv4AsString = JsonConvert.DeserializeObject<IpLog>(response).Ipv4;

        if (String.IsNullOrEmpty(ipv4AsString)) throw new Exception("Empty IPv4 from firebase");

        return ipv4AsString;
    }

    public async Task<int> SaveIP(string ip, CancellationToken token = default)
    {
        IpLog ipLog = new()
        {
            Ipv4 = ip,
            ModifiedAt = DateTime.Now
        };
        await Task.Delay(5000, token);
        // var response = await _httpClient.PutAsJsonAsync(BuildPath("IpLog"), ipLog, token);
        // if (!response.IsSuccessStatusCode)
        // {
        //     throw new Exception("Update firebase didnt success");
        // }
        _logger.Info($"Update firebase successful: {ipLog.ToStringJson()}");
        return (int)HttpStatusCode.OK;
    }


    public static string BuildPath(string name) => UrlFirebase.Replace("{nameNode}", name).Replace("{secret}", FireBaseSecret);
}

public class IpLog
{
    public string Ipv4 { get; set; }

    public DateTime? ModifiedAt { get; set; }

    public string ToStringJson()
    {
        return JsonConvert.SerializeObject(this, Formatting.Indented);
    }

}
