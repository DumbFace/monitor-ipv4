namespace Shared.Shared.Common.Interfaces
{
    public interface IPublisher
    {
        Task PublishAsync(string queue, object data, CancellationToken token = default);

    }
}