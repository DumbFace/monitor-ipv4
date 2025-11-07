namespace monitor_ip_4_tool.Interfaces;

public interface IInternetProtocol
{
    Task<string> GetIP4Async();
}