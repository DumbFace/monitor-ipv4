namespace Shared.Shared.Common.Interfaces
{
    public interface IPublishable
    {
        Task PublishMessageAsync<T>(T data, CancellationToken token = default);
    }
}