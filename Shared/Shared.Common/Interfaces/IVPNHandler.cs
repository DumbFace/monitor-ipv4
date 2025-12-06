namespace Shared.Shared.Common.Interfaces
{
    public interface IVPNHandler
    {
        Task RestartService();

        Task UpdateClient(string ipv4);
    }
}
