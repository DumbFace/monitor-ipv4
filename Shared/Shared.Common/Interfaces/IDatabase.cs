namespace Shared.Shared.Common.Interfaces;

public interface IDatabase : IDataCRUD, IDataCycle
{
    Task<bool> CheckIfTableExist();
}
