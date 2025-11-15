
using monitor_ip_4_tool.Constant;
using monitor_ip_4_tool.Interfaces;

namespace monitor_ip_4_tool.Serivces
{
    public class CustomHttpClientFactory : ICustomHttpFactory
    {
        public HttpClient GetHttpClientDefault()
        {
            HttpClientHandler handler = new HttpClientHandler();
            HttpClient client = new HttpClient(handler)
            {
                Timeout = TimeSpan.FromSeconds(HttpClientEnum.DEFAULT_TIMEOUT),
            };
            return client;
        }

        public HttpClient GetIPv4Client()
        {
            HttpClientHandler handler = new HttpClientHandler();
            HttpClient client = new HttpClient(handler)
            {
                Timeout = TimeSpan.FromSeconds(HttpClientEnum.IPV4_SERVICES_TIMEOUT),
            };
            return client;
        }
    }
}