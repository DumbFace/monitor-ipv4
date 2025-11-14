namespace monitor_ip_4_tool.Interfaces
{
    public interface IRetryHandler
    {
        Task ExecuteAsync(Func<CancellationToken, Task> action);

        Task<T> ExecuteAsync<T>(Func<CancellationToken, Task<T>> action);

    }
}