using System;
using Shared.Shared.Common.Models;

namespace Shared.Shared.Common.Interfaces
{
    public interface ISubscriber
    {

        Task SubscribeAsync<T, TConfig>(Func<T, Task> handler, TConfig config, CancellationToken token = default) where TConfig : IMessagingOptions;
    }
}