using Shared.Shared.Common.Models;

namespace Shared.Shared.Common.Interfaces
{
    public interface IPublisher
    {
        Task PublishAsync<T, TConfig>(T data, TConfig option, CancellationToken token = default) where TConfig : IMessagingOptions;

    }
}