namespace monitor_ip_4_tool.Interfaces
{
    public interface IPublishable
    {
        Task PublishMessageAsync<T>(T data);
    }
}