

using System.Net;
using System.Net.Sockets;

using Shared.Shared.Common.Interfaces;

namespace Shared.Shared.Infrastructure.Serivces
{
    public class Ipv4Services : IInternetProtocol
    {
        private readonly HttpClient _httpClient;
        private readonly ILog _logger;
        public Ipv4Services(
            ILog logger,
            ICustomHttpFactory httpClient
        )
        {
            _logger = logger;
            _httpClient = httpClient.GetIPv4Client();
        }

        public async Task<string> GetIP4Async(string url, CancellationToken token)
        {
            var response = (await _httpClient.GetStringAsync(url, token)).Trim();
            _logger.Info($"Ipv4 response: {response}");
            if (String.IsNullOrEmpty(response)) return String.Empty;

            return response;
        }
    }
}
