namespace monitor_ip_4_tool.Interfaces
{
    public interface IRetryPolicy
    {
        Task ExecuteAsync(Func<Task> action);
        
        Task<T> ExecuteAsync<T>(Task<T> action);

    }
}