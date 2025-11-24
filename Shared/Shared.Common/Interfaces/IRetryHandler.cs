namespace Shared.Shared.Common.Interfaces
{
    public interface IRetryHandler
    {
        Task ExecuteAsync(Func<CancellationToken, Task> action);

        Task<T> ExecuteAsync<T>(Func<CancellationToken, Task<T>> action);

    }
}