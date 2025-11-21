namespace monitor_ip_4_tool.Interfaces
{
    public interface ISubscribable
    {
        Task SubscribeMessageAsync<T>(T data);
    }
}