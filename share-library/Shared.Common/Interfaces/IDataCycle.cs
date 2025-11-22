namespace monitor_ip_4_tool.Interfaces
{
    public interface IDataCycle
    {
        Task ConnectDb();

        Task InitDb();

        Task CloseDb();
    }
}