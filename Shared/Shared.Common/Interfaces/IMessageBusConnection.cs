namespace Shared.Shared.Common.Interfaces
{
    public interface IMessageBusConnection<T>
    {
        Task<T> GetConnectionAsync();
    }
}
