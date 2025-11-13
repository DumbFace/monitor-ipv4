namespace monitor_ip_4_tool.Interfaces
{
    public interface IRetryHandler
    {
        // Task ExecuteAsync(Func<Task> action);
        
        Task<T> ExecuteAsync<T>(Func<Task<T>> action);

    }
}