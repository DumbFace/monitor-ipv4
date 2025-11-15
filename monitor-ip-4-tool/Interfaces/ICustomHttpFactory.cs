namespace monitor_ip_4_tool.Interfaces
{
    public interface ICustomHttpFactory
    {
        HttpClient GetHttpClientDefault();

        HttpClient GetIPv4Client();
    }
}