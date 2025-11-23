namespace Shared.Shared.Common.Interfaces
{
    public interface IDataCycle
    {
        Task ConnectDb();

        Task InitDb();

        Task CloseDb();
    }
}