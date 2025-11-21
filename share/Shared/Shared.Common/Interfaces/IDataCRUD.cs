namespace monitor_ip_4_tool.Interfaces
{
    public interface IDataCRUD
    {
        Task<int> SaveIP(string ip, CancellationToken token = default);

        Task<string> GetLastIP(CancellationToken token = default); 
    }
}