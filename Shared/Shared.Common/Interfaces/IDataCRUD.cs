namespace Shared.Shared.Common.Interfaces
{
    public interface IDataCRUD
    {
        Task<int> SaveIP(string ip, CancellationToken token = default);

        Task<string> GetLastIP(CancellationToken token = default);
    }
}
