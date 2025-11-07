namespace monitor_ip_4_tool.Interfaces;

public interface IDatabase
{
    Task ConnectDb();
    
    Task InitDb();

    Task<int> SaveIP(string ip);
    
    Task<string> GetLastIP();

    Task CloseDb();
}