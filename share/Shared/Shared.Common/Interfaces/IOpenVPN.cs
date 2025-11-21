namespace monitor_ip_4_tool.Interfaces
{
    public interface IOpenVPN
    {
        Task RestartService(string serviceName);

        Task UpdateClient(string ipv4);
    }
}