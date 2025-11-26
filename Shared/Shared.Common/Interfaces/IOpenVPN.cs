namespace Shared.Shared.Common.Interfaces
{
    public interface IOpenVPN
    {
        Task RestartService(string serviceName);

        Task UpdateClient(string ipv4);
    }
}
